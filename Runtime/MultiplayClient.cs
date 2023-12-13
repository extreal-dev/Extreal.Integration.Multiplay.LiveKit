using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.Messaging.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extreal.Integration.Multiplay.Common
{
    public class MultiplayClient : MonoBehaviour
    {
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject[] networkObjects;

        public NetworkClient LocalClient { get; private set; }

        public IReadOnlyDictionary<string, NetworkClient> ConnectedClients => connectedClients;
        private readonly Dictionary<string, NetworkClient> connectedClients = new Dictionary<string, NetworkClient>();

        public IObservable<string> OnConnected => messagingClient.OnConnected;
        public IObservable<string> OnDisconnecting => messagingClient.OnDisconnecting;
        public IObservable<string> OnUnexpectedDisconnected => messagingClient.OnUnexpectedDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected => messagingClient.OnConnectionApprovalRejected;
        public IObservable<string> OnUserConnected => onUserConnected;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onUserConnected = new Subject<string>();
        public IObservable<string> OnUserDisconnected => messagingClient.OnUserDisconnecting;
        public IObservable<(string userId, GameObject networkObject, string message)> OnObjectSpawned => onObjectSpawned;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, GameObject, string)> onObjectSpawned = new Subject<(string, GameObject, string)>();
        public IObservable<(string from, string message)> OnMessageReceived => onMessageReceived;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, string)> onMessageReceived = new Subject<(string, string)>();

        // user自身が持っている自身のplayerのNetworkObjectInfoと他(player以外)のNetworkObjectInfo
        private readonly Dictionary<Guid, NetworkObjectInfo> localNetworkObjectInfoDic = new Dictionary<Guid, NetworkObjectInfo>();

        // 自身の所でspawnした全userのplayer gameObjectと全userのplayer以外のgameObject
        private readonly Dictionary<Guid, GameObject> networkGameObjects = new Dictionary<Guid, GameObject>();

        // spawnに使うplayer prefabとplayer以外のprefab
        private readonly Dictionary<int, GameObject> networkObjectPrefabs = new Dictionary<int, GameObject>();

        [SuppressMessage("Usage", "CC0033")]
        private QueuingMessagingClient messagingClient;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(MultiplayClient));

        private void Awake()
            => Initialize();

        private void Initialize()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Initialize));
            }
            onObjectSpawned.AddTo(this);
            onMessageReceived.AddTo(this);

            AddToNetworkObjectPrefabs(playerObject);
            Array.ForEach(networkObjects, AddToNetworkObjectPrefabs);

            DontDestroyOnLoad(this);
        }

        private void AddToNetworkObjectPrefabs(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            var selfInstanceId = GetGameObjectHash(go);
            networkObjectPrefabs.Add(selfInstanceId, go);
        }

        public void SetMessagingClient(QueuingMessagingClient messagingClient)
        {
            this.messagingClient = messagingClient.AddTo(this);

            this.messagingClient.OnConnected.Subscribe(userId =>
            {
                LocalClient = new NetworkClient(userId);
                connectedClients[userId] = LocalClient;
            });

            messagingClient.OnDisconnecting
                .Merge(messagingClient.OnUnexpectedDisconnected)
                .TakeUntilDestroy(this)
                .Subscribe(_ => Clear());

            messagingClient.OnUserConnected
                .TakeUntilDestroy(this)
                .Subscribe(connectedUserId =>
                {
                    connectedClients[connectedUserId] = new NetworkClient(connectedUserId);

                    var networkObjectInfos = localNetworkObjectInfoDic.Values.ToArray();
                    var message = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjectInfos: networkObjectInfos));
                    messagingClient.EnqueueRequest(message, connectedUserId);
                });

            messagingClient.OnUserDisconnecting
                .TakeUntilDestroy(this)
                .Subscribe(disconnectingUserId =>
                {
                    if (connectedClients.TryGetValue(disconnectingUserId, out var networkClient))
                    {
                        if (networkClient.PlayerObject != null)
                        {
                            Destroy(networkClient.PlayerObject);
                        }
                        foreach (var networkObject in networkClient.NetworkObjects)
                        {
                            Destroy(networkObject);
                        }
                        connectedClients.Remove(disconnectingUserId);
                    }
                });
        }

        private void Clear()
        {
            foreach (var networkGameObject in networkGameObjects.Values)
            {
                Destroy(networkGameObject);
            }

            LocalClient = null;
            connectedClients.Clear();
            localNetworkObjectInfoDic.Clear();
            networkGameObjects.Clear();
        }

        private void Update()
        {
            if (messagingClient == null || !messagingClient.IsConnected)
            {
                return;
            }

            SynchronizeToOthers();
            SynchronizeLocal();
        }

        private void SynchronizeToOthers()
        {
            var networkObjectInfosToSend = new List<NetworkObjectInfo>();
            foreach ((var guid, var networkObjectInfo) in localNetworkObjectInfoDic)
            {
                var localGameObject = networkGameObjects[guid];
                networkObjectInfo.GetTransformFrom(localGameObject.transform);

                if (localGameObject.TryGetComponent(out MultiplayPlayerInput input))
                {
                    networkObjectInfo.GetValuesFrom(in input);
                }

                networkObjectInfo.Updated();

                if (networkObjectInfo.CheckWhetherToSendData())
                {
                    networkObjectInfosToSend.Add(networkObjectInfo);
                }
            }
            if (networkObjectInfosToSend.Count > 0)
            {
                var message = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.Update, networkObjectInfos: networkObjectInfosToSend.ToArray()));
                messagingClient.EnqueueRequest(message);
            }
        }

        private void SynchronizeLocal()
        {
            while (messagingClient.ResponseQueueCount() > 0)
            {
                (var from, var messageJson) = messagingClient.DequeueResponse();
                var message = JsonUtility.FromJson<MultiplayMessage>(messageJson);
                if (localNetworkObjectInfoDic.ContainsKey(message.NetworkObjectInfo.ObjectGuid))
                {
                    continue;
                }

                if (message.Command is MultiplayMessageCommand.Create)
                {
                    CreateObject(from, message.NetworkObjectInfo, message.Message);
                }
                else if (message.Command is MultiplayMessageCommand.Update)
                {
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        UpdateObject(networkObjectInfo);
                    }
                }
                else if (message.Command is MultiplayMessageCommand.CreateExistedObject)
                {
                    connectedClients[from] = new NetworkClient(from);
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        CreateObject(from, networkObjectInfo);
                    }
                    var responseMsg = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.UserInitialized));
                    messagingClient.EnqueueRequest(responseMsg, from);
                }
                else if (message.Command is MultiplayMessageCommand.UserInitialized)
                {
                    onUserConnected.OnNext(from);
                }
                else if (message.Command is MultiplayMessageCommand.Message)
                {
                    onMessageReceived.OnNext((from, message.Message));
                }
            }
        }

        private void CreateObject(string userId, NetworkObjectInfo networkObjectInfo, string message = default)
        {
            var gameObjectHash = networkObjectInfo.GameObjectHash;
            if (Logger.IsDebug())
            {
                Logger.LogDebug(
                    "Create network object:"
                    + $" Participant={userId}, CreatedAt={networkObjectInfo.CreatedAt}, ObjectGuid={networkObjectInfo.ObjectGuid}, InstanceId={gameObjectHash}");
            }

            var prefab = networkObjectPrefabs[gameObjectHash];
            var setToNetworkClient = (Action<GameObject>)(
                gameObjectHash == GetGameObjectHash(playerObject)
                    ? connectedClients[userId].SetPlayerObject
                    : connectedClients[userId].AddNetworkObject
            );

            SpawnInternal(prefab, networkObjectInfo, setToNetworkClient, userId, message: message);
        }

        private void UpdateObject(NetworkObjectInfo obj)
        {
            if (networkGameObjects.TryGetValue(obj.ObjectGuid, out var objectToBeUpdated))
            {
                if (objectToBeUpdated.TryGetComponent(out MultiplayPlayerInput input))
                {
                    obj.ApplyValuesTo(in input);
                }

                if ((objectToBeUpdated.transform.position - obj.Position).sqrMagnitude > 0f)
                {
                    objectToBeUpdated.transform.position = obj.Position;
                    objectToBeUpdated.transform.rotation = obj.Rotation;
                }
            }
        }

        public UniTask ConnectAsync(MessagingConnectionConfig connectionConfig)
        {
            CheckTransport();
            return messagingClient.ConnectAsync(connectionConfig);
        }

        public void Disconnect()
        {
            CheckTransport();

            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Disconnect));
            }
            messagingClient.DisconnectAsync();
        }

        public GameObject SpawnPlayer(Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            if (playerObject == null)
            {
                throw new InvalidOperationException("Add an object to use as player to the playerObject of this instance");
            }

            var selfInstanceId = GetGameObjectHash(playerObject);
            var networkObjectInfo = new NetworkObjectInfo(selfInstanceId, position, rotation);
            return SpawnInternal(playerObject, networkObjectInfo, LocalClient.SetPlayerObject, LocalClient.UserId, parent, message);
        }

        public GameObject SpawnObject(GameObject objectPrefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            if (!networkObjectPrefabs.ContainsKey(GetGameObjectHash(objectPrefab)))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects you have added to the networkObjects of this instance");
            }

            var networkObjectInfo = new NetworkObjectInfo(GetGameObjectHash(objectPrefab), position, rotation);
            return SpawnInternal(objectPrefab, networkObjectInfo, LocalClient.AddNetworkObject, LocalClient.UserId, parent, message);
        }

        private GameObject SpawnInternal
        (
            GameObject prefab,
            NetworkObjectInfo networkObjectInfo,
            Action<GameObject> setToNetworkClient,
            string userId,
            Transform parent = default,
            string message = default
        )
        {
            CheckTransport();

            var spawnedObject = Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation, parent);
            setToNetworkClient.Invoke(spawnedObject);
            networkGameObjects.Add(networkObjectInfo.ObjectGuid, spawnedObject);
            if (userId == LocalClient?.UserId)
            {
                localNetworkObjectInfoDic.Add(networkObjectInfo.ObjectGuid, networkObjectInfo);
                var messageJson = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.Create, networkObjectInfo: networkObjectInfo, message: message));
                messagingClient.EnqueueRequest(messageJson);
            }

            onObjectSpawned.OnNext((userId, spawnedObject, message));
            return spawnedObject;
        }

        private static int GetGameObjectHash(GameObject target)
        {
            var id = GetHierarchyPath(target);
            return id.GetHashCode();
        }

        private static string GetHierarchyPath(GameObject target)
        {
            var path = string.Empty;
            var current = target.transform;

            while (current != null)
            {
                var index = current.GetSiblingIndex();
                path = "/" + current.name + index + path;
                current = current.parent;
            }
            var belongScene = GetBelongScene(target);
            return "/" + belongScene.name + path;
        }

        private static Scene GetBelongScene(GameObject target)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid())
                {
                    continue;
                }
                if (!scene.isLoaded)
                {
                    continue;
                }
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root == target.transform.root.gameObject)
                    {
                        return scene;
                    }
                }
            }
            return default;
        }

        public void SendMessage(string message, string to = default)
        {
            CheckTransport();

            var messageJson = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.Message, message: message));
            messagingClient.EnqueueRequest(messageJson, to);
        }

        private void CheckTransport()
        {
            if (messagingClient == null)
            {
                throw new InvalidOperationException("Set Transport before this operation.");
            }
        }
    }
}
