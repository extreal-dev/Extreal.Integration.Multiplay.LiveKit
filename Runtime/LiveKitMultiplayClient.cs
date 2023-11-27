using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using LiveKit;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class LiveKitMultiplayClient : MonoBehaviour
    {
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject[] networkObjects;

        public NetworkClient LocalClient { get; private set; }

        public IReadOnlyDictionary<Participant, NetworkClient> ConnectedClients => connectedClients;
        private readonly Dictionary<Participant, NetworkClient> connectedClients = new Dictionary<Participant, NetworkClient>();

        public IObservable<Unit> OnConnected => transport.OnConnected;
        public IObservable<Unit> OnDisconnecting => transport.OnDisconnecting;
        public IObservable<DisconnectReason> OnUnexpectedDisconnected => transport.OnUnexpectedDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected => transport.OnConnectionApprovalRejected;
        public IObservable<RemoteParticipant> OnUserConnected => transport.OnUserConnected;
        public IObservable<RemoteParticipant> OnUserDisconnected => transport.OnUserDisconnected;
        public IObservable<(Participant participant, GameObject networkObject)> OnObjectSpawned => onObjectSpawned;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(Participant, GameObject)> onObjectSpawned = new Subject<(Participant, GameObject)>();
        public IObservable<(Participant participant, string message)> OnMessageReceived => transport.OnMessageReceived;

        private readonly Dictionary<Guid, NetworkObjectInfo> localNetworkObjectInfos = new Dictionary<Guid, NetworkObjectInfo>();
        private readonly Dictionary<Guid, GameObject> networkGameObjects = new Dictionary<Guid, GameObject>();

        private readonly Dictionary<int, GameObject> networkObjectPrefabs = new Dictionary<int, GameObject>();

        [SuppressMessage("Usage", "CC0033")]
        private readonly LiveKitMultiplayTransport transport = new LiveKitMultiplayTransport();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(LiveKitMultiplayClient));

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

            transport.OnDisconnecting
                .Merge(transport.OnUnexpectedDisconnected.Select(_ => Unit.Default))
                .TakeUntilDestroy(this)
                .Subscribe(_ => Clear());

            transport.OnUserConnected
                .TakeUntilDestroy(this)
                .Subscribe(participant =>
                {
                    connectedClients[participant] = new NetworkClient(participant);
                });

            transport.OnUserDisconnected
                .TakeUntilDestroy(this)
                .Subscribe(participant =>
                {
                    var networkClient = connectedClients[participant];
                    if (networkClient.PlayerObject != null)
                    {
                        Destroy(networkClient.PlayerObject);
                    }
                    foreach (var networkObject in networkClient.NetworkObjects)
                    {
                        Destroy(networkObject);
                    }
                    connectedClients.Remove(participant);
                });

            AddToNetworkObjectPrefabs(playerObject);
            Array.ForEach(networkObjects, AddToNetworkObjectPrefabs);
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

                if (localGameObject.TryGetComponent(out LiveKitPlayerInput input))
                {
                    networkObjectInfo.GetValuesFrom(in input);
                }

                networkObjectInfo.Updated();

                var message = new LiveKitMultiplayMessage(LiveKidMultiplayMessageCommand.Update, networkObjectInfo);
                transport.EnqueueRequest(message);
            }

            while (transport.ResponseQueueCount() > 0)
            {
                (var participant, var message) = transport.DequeueResponse();
                if (message == null)
                {
                    continue;
                }

                var networkObjectInfo = message.Payload;
                if (localNetworkObjectInfos.ContainsKey(networkObjectInfo.ObjectGuid))
                {
                    continue;
                }

                if (message.LiveKidMultiplayMessageCommand is LiveKidMultiplayMessageCommand.Create)
                {
                    CreateObject(participant, networkObjectInfo);
                }
                else if (message.LiveKidMultiplayMessageCommand is LiveKidMultiplayMessageCommand.Update)
                {
                    UpdateObject(networkObjectInfo);
                }
            }

            transport.Update();
        }

        private void CreateObject(Participant participant, NetworkObjectInfo networkObjectInfo)
        {
            var instanceId = networkObjectInfo.InstanceId;
            if (Logger.IsDebug())
            {
                Logger.LogDebug(
                    "Create network object:"
                    + $" Participant={participant.Name}, CreatedAt={networkObjectInfo.CreatedAt}, ObjectGuid={networkObjectInfo.ObjectGuid}, InstanceId={instanceId}");
            }

            var prefab = networkObjectPrefabs[instanceId];
            var setToNetworkClient = (Action<GameObject>)(
                instanceId == playerObject.GetInstanceID()
                    ? connectedClients[participant].SetPlayerObject
                    : connectedClients[participant].AddNetworkObject
            );

            SpawnInternal(prefab, networkObjectInfo, setToNetworkClient, participant);
        }

        private void UpdateObject(NetworkObjectInfo obj)
        {
            if (networkGameObjects.TryGetValue(obj.ObjectGuid, out var objectToBeUpdated))
            {
                if (objectToBeUpdated.TryGetComponent(out LiveKitPlayerInput input))
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

            var participant = await transport.ConnectAsync(connectionConfig);
            LocalClient = new NetworkClient(participant);
            connectedClients[participant] = LocalClient;
        }

        public void Disconnect()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Disconnect));
            }

            transport.Disconnect();
        }

        public UniTask DeleteRoomAsync()
            => transport.DeleteRoomAsync();

        public GameObject SpawnPlayer(Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            if (playerObject == null)
            {
                throw new InvalidOperationException("Add an object to use as player to the playerObject of this instance");
            }

            var networkObjectInfo = new NetworkObjectInfo(playerObject.GetInstanceID(), position, rotation);
            return SpawnInternal(playerObject, networkObjectInfo, LocalClient.SetPlayerObject, LocalClient.Participant, parent);
        }

        public GameObject SpawnObject(GameObject objectPrefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            if (!networkObjectPrefabs.ContainsKey(objectPrefab.GetInstanceID()))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects you have added to the networkObjects of this instance");
            }

            var networkObjectInfo = new NetworkObjectInfo(objectPrefab.GetInstanceID(), position, rotation);
            return SpawnInternal(objectPrefab, networkObjectInfo, LocalClient.AddNetworkObject, LocalClient.Participant, parent);
        }

        private GameObject SpawnInternal
        (
            GameObject prefab,
            NetworkObjectInfo networkObjectInfo,
            Action<GameObject> setToNetworkClient,
            Participant participant,
            Transform parent = default
        )
        {
            var spawnedObject = Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation, parent);
            setToNetworkClient?.Invoke(spawnedObject);
            networkGameObjects.Add(networkObjectInfo.ObjectGuid, spawnedObject);

            if (participant == LocalClient.Participant)
            {
                localNetworkObjectInfos.Add(networkObjectInfo.ObjectGuid, networkObjectInfo);
                var message = new LiveKitMultiplayMessage(LiveKidMultiplayMessageCommand.Create, networkObjectInfo);
                transport.EnqueueRequest(message);
            }

            onObjectSpawned.OnNext((participant, spawnedObject));

            return spawnedObject;
        }

        public void SendMessage(string message, DataPacketKind dataPacketKind = DataPacketKind.RELIABLE)
            => transport.SendMessageAsync(message, dataPacketKind).Forget();

        public UniTask<string[]> ListRoomsAsync()
            => transport.ListRoomsAsync();
    }

#if !UNITY_WEBGL // || UNITY_EDITOR
    public class LiveKitMultiplayTransport : NativeLiveKitMultiplayTransport
    {
        public LiveKitMultiplayTransport() : base() { }
    }
#else
    public class LiveKitMultiplayTransport : WebGLLiveKitMultiplayTransport
    {
        public LiveKitMultiplayTransport() : base() { }
    };
#endif
}
