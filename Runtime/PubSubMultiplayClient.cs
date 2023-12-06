using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public class PubSubMultiplayClient : MonoBehaviour
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
        private readonly Subject<string> onUserConnected = new Subject<string>();
        public IObservable<string> OnUserDisconnected => transport.OnUserDisconnecting;
        public IObservable<(string userIdentity, GameObject networkObject, string message)> OnObjectSpawned => onObjectSpawned;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, GameObject, string)> onObjectSpawned = new Subject<(string, GameObject, string)>();
        public IObservable<(string userIdentityRemote, string message)> OnMessageReceived => transport.OnMessageReceived;

        // user自身が持っている自身のplayerのNetworkObjectInfoと他(player以外)のNetworkObjectInfo
        private readonly Dictionary<Guid, NetworkObjectInfo> localNetworkObjectInfoDic = new Dictionary<Guid, NetworkObjectInfo>();

        // 全userのplayer gameObjectと全userのplayer以外のgameObject
        private readonly Dictionary<Guid, GameObject> networkGameObjects = new Dictionary<Guid, GameObject>();

        // 自身アプリの所でspawnした全player gameObjectと全userのplayer以外のgameObject
        private readonly Dictionary<int, GameObject> networkObjectPrefabs = new Dictionary<int, GameObject>();

        [SuppressMessage("Usage", "CC0033")]
        private IExtrealMultiplayTransport transport;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(PubSubMultiplayClient));

        public void Awake()
            => Initialize();

        private void Initialize()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Initialize));
            }
            onObjectSpawned.AddTo(this);
            transport.AddTo(this);

            transport.OnConnected.Subscribe(userIdentityLocal =>
            {
                LocalClient = new NetworkClient(userIdentityLocal);
                connectedClients[userIdentityLocal] = LocalClient;
            });

            transport.OnDisconnecting
                .Merge(transport.OnUnexpectedDisconnected)
                .TakeUntilDestroy(this)
                .Subscribe(_ => Clear());

            transport.OnUserConnected
                .TakeUntilDestroy(this)
                .Subscribe(userIdentityRemote =>
                {
                    connectedClients[userIdentityRemote] = new NetworkClient(userIdentityRemote);

                    var networkObjectInfos = localNetworkObjectInfoDic.Values.ToArray();
                    var message = new MultiplayMessage(MultiplayMessageCommand.UserConnected, networkObjectInfos: networkObjectInfos);
                    transport.EnqueueRequest(message, userIdentityRemote);
                });

            transport.OnUserDisconnecting
                .TakeUntilDestroy(this)
                .Subscribe(userIdentityRemote =>
                {
                    var networkClient = connectedClients[userIdentityRemote];
                    if (networkClient.PlayerObject != null)
                    {
                        Destroy(networkClient.PlayerObject);
                    }
                    foreach (var networkObject in networkClient.NetworkObjects)
                    {
                        Destroy(networkObject);
                    }
                    connectedClients.Remove(userIdentityRemote);
                });

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
            var selfInstanceId = Utility.GetGameObjectHash(go);
            networkObjectPrefabs.Add(selfInstanceId, go);
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
            if (!transport.IsConnected)
            {
                return;
            }

            var networkObjectInfosToSend = new List<NetworkObjectInfo>();
            foreach ((var guid, var networkObjectInfo) in localNetworkObjectInfoDic)
            {
                var localGameObject = networkGameObjects[guid];
                networkObjectInfo.GetTransformFrom(localGameObject.transform);

                if (localGameObject.TryGetComponent(out RedisPlayerInput input))
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
                var message = new MultiplayMessage(MultiplayMessageCommand.Update, networkObjectInfos: networkObjectInfosToSend.ToArray());
                transport.EnqueueRequest(message);
            }

            while (transport.ResponseQueueCount() > 0)
            {
                (var userIdentityRemote, var message) = transport.DequeueResponse();
                if (message == null)
                {
                    continue;
                }

                if (localNetworkObjectInfoDic.ContainsKey(message.NetworkObjectInfo.ObjectGuid))
                {
                    continue;
                }

                if (message.MultiplayMessageCommand is MultiplayMessageCommand.Create)
                {
                    CreateObject(userIdentityRemote, message.NetworkObjectInfo, message.Message);
                }
                else if (message.MultiplayMessageCommand is MultiplayMessageCommand.Update)
                {
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        UpdateObject(networkObjectInfo);
                    }
                }
                else if (message.MultiplayMessageCommand is MultiplayMessageCommand.UserConnected)
                {
                    Debug.LogWarning($"user connected: {userIdentityRemote}");
                    connectedClients[userIdentityRemote] = new NetworkClient(userIdentityRemote);
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        CreateObject(userIdentityRemote, networkObjectInfo);
                    }
                    var msg = new MultiplayMessage(MultiplayMessageCommand.UserInitialized);
                    transport.EnqueueRequest(msg, userIdentityRemote);
                }
                else if (message.MultiplayMessageCommand is MultiplayMessageCommand.UserInitialized)
                {
                    onUserConnected.OnNext(userIdentityRemote);
                }
            }

            transport.UpdateAsync().Forget();
        }

        private void CreateObject(string userIdentity, NetworkObjectInfo networkObjectInfo, string message = default)
        {
            var gameObjectHash = networkObjectInfo.InstanceId;
            if (Logger.IsDebug())
            {
                Logger.LogDebug(
                    "Create network object:"
                    + $" Participant={userIdentity}, CreatedAt={networkObjectInfo.CreatedAt}, ObjectGuid={networkObjectInfo.ObjectGuid}, InstanceId={gameObjectHash}");
            }

            var prefab = networkObjectPrefabs[gameObjectHash];
            var setToNetworkClient = (Action<GameObject>)(
                gameObjectHash == Utility.GetGameObjectHash(playerObject)
                    ? connectedClients[userIdentity].SetPlayerObject
                    : connectedClients[userIdentity].AddNetworkObject
            );

            SpawnInternal(prefab, networkObjectInfo, setToNetworkClient, userIdentity, message: message);
        }

        private void UpdateObject(NetworkObjectInfo obj)
        {
            if (networkGameObjects.TryGetValue(obj.ObjectGuid, out var objectToBeUpdated))
            {
                if (objectToBeUpdated.TryGetComponent(out RedisPlayerInput input))
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

        public void SetTransport(IExtrealMultiplayTransport transport)
            => this.transport = transport;

        public UniTask ConnectAsync(MultiplayConnectionConfig connectionConfig)
            => transport.ConnectAsync(connectionConfig);

        public void Disconnect()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Disconnect));
            }

            transport.DisconnectAsync();
        }

        public UniTask DeleteRoomAsync()
            => transport.DeleteRoomAsync();

        public GameObject SpawnPlayer(Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            var selfInstanceId = Utility.GetGameObjectHash(playerObject);
            if (playerObject == null)
            {
                throw new InvalidOperationException("Add an object to use as player to the playerObject of this instance");
            }

            var networkObjectInfo = new NetworkObjectInfo(selfInstanceId, position, rotation);
            return SpawnInternal(playerObject, networkObjectInfo, LocalClient.SetPlayerObject, LocalClient.UserIdentity, parent, message);
        }

        public GameObject SpawnObject(GameObject objectPrefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            if (!networkObjectPrefabs.ContainsKey(objectPrefab.GetInstanceID()))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects you have added to the networkObjects of this instance");
            }

            var networkObjectInfo = new NetworkObjectInfo(objectPrefab.GetInstanceID(), position, rotation);
            return SpawnInternal(objectPrefab, networkObjectInfo, LocalClient.AddNetworkObject, LocalClient.UserIdentity, parent, message);
        }

        private GameObject SpawnInternal
        (
            GameObject prefab,
            NetworkObjectInfo networkObjectInfo,
            Action<GameObject> setToNetworkClient,
            string userIdentity,
            Transform parent = default,
            string message = default
        )
        {
            var spawnedObject = Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation, parent);
            setToNetworkClient.Invoke(spawnedObject);
            networkGameObjects.Add(networkObjectInfo.ObjectGuid, spawnedObject);
            if (userIdentity == LocalClient?.UserIdentity)
            {
                localNetworkObjectInfoDic.Add(networkObjectInfo.ObjectGuid, networkObjectInfo);
                var multiplayMessage = new MultiplayMessage(MultiplayMessageCommand.Create, networkObjectInfo: networkObjectInfo, message: message);
                transport.EnqueueRequest(multiplayMessage);
            }

            onObjectSpawned.OnNext((userIdentity, spawnedObject, message));
            return spawnedObject;
        }

        public void SendMessage(string message, MultiplayMessageCommand command = MultiplayMessageCommand.Message)
            => transport.EnqueueRequest(new MultiplayMessage(command, message: message));

        public void SendMessage(string toUserIdentity, string message, MultiplayMessageCommand command = MultiplayMessageCommand.Message)
            => transport.EnqueueRequest(new MultiplayMessage(command, message: message), toUserIdentity);

        public UniTask<List<MultiplayRoomInfo>> ListRoomsAsync()
            => transport.ListRoomsAsync();
    }
}
