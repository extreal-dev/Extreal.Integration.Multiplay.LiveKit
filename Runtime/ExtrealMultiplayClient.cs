using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extreal.Integration.Multiplay.Common
{
    public class ExtrealMultiplayClient : MonoBehaviour
    {
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject[] networkObjects;

        public NetworkClient LocalClient { get; private set; }

        public IReadOnlyDictionary<string, NetworkClient> ConnectedClients => connectedClients;
        private readonly Dictionary<string, NetworkClient> connectedClients = new Dictionary<string, NetworkClient>();

        public IObservable<string> OnConnected => transport.OnConnected;
        public IObservable<string> OnDisconnecting => transport.OnDisconnecting;
        public IObservable<string> OnUnexpectedDisconnected => transport.OnUnexpectedDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected => transport.OnConnectionApprovalRejected;
        public IObservable<string> OnUserConnected => onUserConnected;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onUserConnected = new Subject<string>();
        public IObservable<string> OnUserDisconnected => transport.OnUserDisconnecting;
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
        private IExtrealMultiplayTransport transport;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(ExtrealMultiplayClient));

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

        public void SetTransport(IExtrealMultiplayTransport transport)
        {
            this.transport = transport.AddTo(this);

            this.transport.OnConnected.Subscribe(userId =>
            {
                LocalClient = new NetworkClient(userId);
                connectedClients[userId] = LocalClient;
            });

            this.transport.OnDisconnecting
                .Merge(this.transport.OnUnexpectedDisconnected)
                .TakeUntilDestroy(this)
                .Subscribe(_ => Clear());

            this.transport.OnUserConnected
                .TakeUntilDestroy(this)
                .Subscribe(connectedUserId =>
                {
                    connectedClients[connectedUserId] = new NetworkClient(connectedUserId);

                    var networkObjectInfos = localNetworkObjectInfoDic.Values.ToArray();
                    var message = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjectInfos: networkObjectInfos));
                    this.transport.EnqueueRequest(message, connectedUserId);
                });

            this.transport.OnUserDisconnecting
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
            if (transport == null || !transport.IsConnected)
            {
                return;
            }

            SynchronizeToOthers();
            SynchronizeLocal();

            transport.UpdateAsync().Forget();
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
                transport.EnqueueRequest(message);
            }
        }

        private void SynchronizeLocal()
        {
            while (transport.ResponseQueueCount() > 0)
            {
                (var from, var messageJson) = transport.DequeueResponse();
                var message = JsonUtility.FromJson<MultiplayMessage>(messageJson);
                if (localNetworkObjectInfoDic.ContainsKey(message.NetworkObjectInfo.ObjectGuid))
                {
                    continue;
                }

                if (message.MultiplayMessageCommand is MultiplayMessageCommand.Create)
                {
                    CreateObject(from, message.NetworkObjectInfo, message.Message);
                }
                else if (message.MultiplayMessageCommand is MultiplayMessageCommand.Update)
                {
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        UpdateObject(networkObjectInfo);
                    }
                }
                else if (message.MultiplayMessageCommand is MultiplayMessageCommand.CreateExistedObject)
                {
                    connectedClients[from] = new NetworkClient(from);
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        CreateObject(from, networkObjectInfo);
                    }
                    var responseMsg = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.UserInitialized));
                    transport.EnqueueRequest(responseMsg, from);
                }
                else if (message.MultiplayMessageCommand is MultiplayMessageCommand.UserInitialized)
                {
                    onUserConnected.OnNext(from);
                }
                else if (message.MultiplayMessageCommand is MultiplayMessageCommand.Message)
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

        public UniTask<List<Room>> ListRoomsAsync()
        {
            CheckTransport();
            return transport.ListRoomsAsync();
        }

        public UniTask ConnectAsync(MultiplayConnectionConfig connectionConfig)
        {
            CheckTransport();
            return transport.ConnectAsync(connectionConfig);
        }

        public void Disconnect()
        {
            CheckTransport();

            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Disconnect));
            }
            transport.DisconnectAsync();
        }

        public UniTask DeleteRoomAsync()
        {
            CheckTransport();
            return transport.DeleteRoomAsync();
        }

        public GameObject SpawnPlayer(Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            var selfInstanceId = GetGameObjectHash(playerObject);
            if (playerObject == null)
            {
                throw new InvalidOperationException("Add an object to use as player to the playerObject of this instance");
            }

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
                transport.EnqueueRequest(messageJson);
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
            transport.EnqueueRequest(messageJson, to);
        }

        private void CheckTransport()
        {
            if (transport == null)
            {
                throw new InvalidOperationException("Set Transport before this operation.");
            }
        }
    }
}
