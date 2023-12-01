using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class PubSubMultiplayClient : MonoBehaviour
    {
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject[] networkObjects;

        public NetworkClient LocalClient { get; private set; }

        public string Topic { get; private set; }
        public IReadOnlyDictionary<string, NetworkClient> ConnectedClients => connectedClients;
        private readonly Dictionary<string, NetworkClient> connectedClients = new Dictionary<string, NetworkClient>();

        public IObservable<string> OnConnected => transport.OnConnected;
        public IObservable<Unit> OnDisconnecting => transport.OnDisconnecting;
        public IObservable<string> OnUnexpectedDisconnected => transport.OnUnexpectedDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected => transport.OnConnectionApprovalRejected;
        public IObservable<string> OnUserConnected => transport.OnUserConnected;
        public IObservable<string> OnUserDisconnected => transport.OnUserDisconnecting;
        public IObservable<(string userIdentity, GameObject networkObject)> OnObjectSpawned => onObjectSpawned;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, GameObject)> onObjectSpawned = new Subject<(string, GameObject)>();
        public IObservable<(string userIdentityRemote, string message)> OnMessageReceived => transport.OnMessageReceived;

        private readonly Dictionary<Guid, NetworkObjectInfo> localNetworkObjectInfos = new Dictionary<Guid, NetworkObjectInfo>();
        private readonly Dictionary<Guid, GameObject> networkGameObjects = new Dictionary<Guid, GameObject>();

        private readonly Dictionary<int, GameObject> networkObjectPrefabs = new Dictionary<int, GameObject>();

        [SuppressMessage("Usage", "CC0033")]
        private readonly NativeMultiplayTransport transport = new NativeMultiplayTransport();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(PubSubMultiplayClient));

        public void Awake()
            => Initialize();

        private void Initialize()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Initialize));
            }
            Debug.LogWarning(playerObject);
            Logger.LogDebug("!!!!!!!!Debug.LogWarning(playerObject);");
            onObjectSpawned.AddTo(this);
            transport.AddTo(this);

            transport.OnConnected.Subscribe(userIdentity =>
            {
                Logger.LogDebug("!!!transport.OnConnected");
                LocalClient = new NetworkClient(userIdentity);
                connectedClients[userIdentity] = LocalClient;
            });

            transport.OnDisconnecting
                .Merge(transport.OnUnexpectedDisconnected.Select(_ => Unit.Default))
                .TakeUntilDestroy(this)
                .Subscribe(_ => Clear());

            transport.OnUserConnected
                .TakeUntilDestroy(this)
                .Subscribe(userIdentityRemote =>
                {
                    connectedClients[userIdentityRemote] = new NetworkClient(userIdentityRemote);

                    var networkObjectInfos = localNetworkObjectInfos.Values.ToArray();
                    var message = new MultiplayMessage(userIdentityRemote, Topic, MultiplayMessageCommand.UserConnected, networkObjectInfos: networkObjectInfos);
                    transport.EnqueueRequest(message);
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
            var instanceId = go.GetInstanceID();
            networkObjectPrefabs.Add(instanceId, go);
        }

        private void Clear()
        {
            foreach (var networkGameObject in networkGameObjects.Values)
            {
                Destroy(networkGameObject);
            }

            LocalClient = null;
            connectedClients.Clear();
            localNetworkObjectInfos.Clear();
            networkGameObjects.Clear();
        }

        private void Update()
        {
            if (!transport.IsConnected)
            {
                return;
            }

            foreach ((var guid, var networkObjectInfo) in localNetworkObjectInfos)
            {
                var localGameObject = networkGameObjects[guid];
                networkObjectInfo.GetTransformFrom(localGameObject.transform);

                if (localGameObject.TryGetComponent(out RedisPlayerInput input))
                {
                    networkObjectInfo.GetValuesFrom(in input);
                }

                networkObjectInfo.Updated();
            }
            if (localNetworkObjectInfos.Count > 0)
            {
                var message = new MultiplayMessage(LocalClient.UserIdentity, Topic, MultiplayMessageCommand.Update, networkObjectInfos: localNetworkObjectInfos.Values.ToArray());
                transport.EnqueueRequest(message);
            }

            while (transport.ResponseQueueCount() > 0)
            {
                (var userIdentityRemote, var message) = transport.DequeueResponse();
                if (message == null)
                {
                    continue;
                }

                if (localNetworkObjectInfos.ContainsKey(message.NetworkObjectInfo.ObjectGuid))
                {
                    continue;
                }

                if (message.MultiplayMessageCommand is MultiplayMessageCommand.Create)
                {
                    CreateObject(userIdentityRemote, message.NetworkObjectInfo);
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
                    connectedClients[userIdentityRemote] = new NetworkClient(userIdentityRemote);
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        CreateObject(userIdentityRemote, networkObjectInfo);
                    }
                }
            }

            transport.Update();
        }

        private void CreateObject(string userIdentity, NetworkObjectInfo networkObjectInfo)
        {
            var instanceId = networkObjectInfo.InstanceId;
            if (Logger.IsDebug())
            {
                Logger.LogDebug(
                    "Create network object:"
                    + $" Participant={userIdentity}, CreatedAt={networkObjectInfo.CreatedAt}, ObjectGuid={networkObjectInfo.ObjectGuid}, InstanceId={instanceId}");
            }

            var prefab = networkObjectPrefabs[instanceId];
            var setToNetworkClient = (Action<GameObject>)(
                instanceId == playerObject.GetInstanceID()
                    ? connectedClients[userIdentity].SetPlayerObject
                    : connectedClients[userIdentity].AddNetworkObject
            );

            SpawnInternal(prefab, networkObjectInfo, setToNetworkClient, userIdentity);
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

        public void Initialize(TransportConfig transportConfig = default)
            => transport.Initialize(transportConfig);

        public async UniTask ConnectAsync(ConnectionConfig connectionConfig)
        {
            if (transport.IsConnected)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn("This client is already connected.");
                }
                return;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: url={connectionConfig.Url}");
            }
            await transport.ConnectAsync(connectionConfig);
        }

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

        public GameObject SpawnPlayer(Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            Logger.LogDebug("!!! IN SpawnPlayer");
            Debug.LogWarning(playerObject.name);
            Debug.LogWarning(playerObject == null);
            Debug.LogWarning($"!!! IN SpawnPlayer playerObject is {playerObject}");
            var instanceId = playerObject.GetInstanceID();
            Logger.LogDebug($"!!! IN SpawnPlayer instanceId is {instanceId}");
            if (playerObject == null)
            {
                throw new InvalidOperationException("Add an object to use as player to the playerObject of this instance");
            }
            Logger.LogDebug($"!!! IN SpawnPlayer Before NetworkObjectInfo");

            var networkObjectInfo = new NetworkObjectInfo(playerObject.GetInstanceID(), position, rotation);
            Logger.LogDebug($"!!! IN SpawnPlayer LocalClient is: id={LocalClient.UserIdentity} instanceId={LocalClient.PlayerObject}");
            return SpawnInternal(playerObject, networkObjectInfo, LocalClient.SetPlayerObject, LocalClient.UserIdentity, parent);
        }

        public GameObject SpawnObject(GameObject objectPrefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            if (!networkObjectPrefabs.ContainsKey(objectPrefab.GetInstanceID()))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects you have added to the networkObjects of this instance");
            }

            var networkObjectInfo = new NetworkObjectInfo(objectPrefab.GetInstanceID(), position, rotation);
            return SpawnInternal(objectPrefab, networkObjectInfo, LocalClient.AddNetworkObject, LocalClient.UserIdentity, parent);
        }

        private GameObject SpawnInternal
        (
            GameObject prefab,
            NetworkObjectInfo networkObjectInfo,
            Action<GameObject> setToNetworkClient,
            string userIdentity,
            Transform parent = default
        )
        {
            Logger.LogDebug("!!! IN SpawnInternal");
            var spawnedObject = Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation, parent);
            setToNetworkClient?.Invoke(spawnedObject);
            networkGameObjects.Add(networkObjectInfo.ObjectGuid, spawnedObject);

            if (userIdentity == LocalClient.UserIdentity)
            {
                localNetworkObjectInfos.Add(networkObjectInfo.ObjectGuid, networkObjectInfo);
                var message = new MultiplayMessage(userIdentity, Topic, MultiplayMessageCommand.Create, networkObjectInfo: networkObjectInfo);
                transport.EnqueueRequest(message);
            }

            onObjectSpawned.OnNext((userIdentity, spawnedObject));
            Logger.LogDebug("!!! After SpawnInternal");
            return spawnedObject;
        }

        public void SendMessage(string userIdentity, string message)
            => transport.EnqueueRequest(new MultiplayMessage(userIdentity, Topic, MultiplayMessageCommand.Message, message: message));

        public UniTask<RoomInfo[]> ListRoomsAsync()
            => transport.ListRoomsAsync();
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    public class LiveKitMultiplayTransport : NativeMultiplayTransport
    {
        public LiveKitMultiplayTransport() : base() { }
    }
#else
    public class LiveKitMultiplayTransport : WebGLMultiplayTransport
    {
        public LiveKitMultiplayTransport() : base() { }
    };
#endif
}
