using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;

using UnityEngine.InputSystem;
using LiveKit;
using Extreal.Core.Logging;
using UniRx;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class LiveKitMultiplayClient : MonoBehaviour
    {
        [SerializeField] private string relayURL = "http://localhost:7881";
        [SerializeField] private string roomName = "MultiplayTest";
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject[] networkObjects;

        public NetworkClient LocalClient { get; private set; }

        public IReadOnlyDictionary<Participant, NetworkClient> ConnectedClients => connectedClients;
        private readonly Dictionary<Participant, NetworkClient> connectedClients = new Dictionary<Participant, NetworkClient>();

        public IObservable<Unit> OnConnected => transport.OnConnected;
        public IObservable<Unit> OnDisconnected => transport.OnDisconnected;
        public IObservable<DisconnectReason> OnUnexpectedDisconnected => transport.OnUnexpectedDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected => transport.OnConnectionApprovalRejected;
        public IObservable<RemoteParticipant> OnUserConnected => transport.OnUserConnected;
        public IObservable<RemoteParticipant> OnUserDisconnected => transport.OnUserDisconnected;
        public IObservable<(Participant participant, GameObject networkObject)> OnObjectSpawned;
        public IObservable<(Participant participant, LiveKidMultiplayMessageContainer message)> OnMessageReceived => transport.OnMessageReceived;

        private readonly Dictionary<Guid, NetworkObjectInfo> localNetworkObjectInfos = new Dictionary<Guid, NetworkObjectInfo>();
        private readonly Dictionary<Guid, GameObject> networkGameObjects = new Dictionary<Guid, GameObject>();

        private readonly Dictionary<int, GameObject> networkObjectPrefabs = new Dictionary<int, GameObject>();

        [SuppressMessage("Usage", "CC0033")]
        private readonly LiveKitMultiplayTransport transport = new LiveKitMultiplayTransport();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(LiveKitMultiplayClient));

        public void Awake()
            => Initialize();

        [SuppressMessage("Usage", "CC0022")]
        private void Initialize()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Initialize));
            }

            transport.AddTo(this);

            transport.OnDisconnected
                .Merge(transport.OnUnexpectedDisconnected.Select(_ => Unit.Default))
                .TakeUntilDestroy(this)
                .Subscribe(_ => Clear());

            transport.OnUserConnected
                .TakeUntilDestroy(this)
                .Subscribe(participant => connectedClients[participant] = new NetworkClient(participant));

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
            var guid = networkObjectInfo.ObjectGuid;
            var instanceId = networkObjectInfo.InstanceId;
            if (Logger.IsDebug())
            {
                Logger.LogDebug(
                    "Create network object:"
                    + $" participant={participant.Name}, CreatedAt={networkObjectInfo.CreatedAt}, ObjectGuid={guid}, instanceId={instanceId}");
            }

            var prefab = networkObjectPrefabs[instanceId];
            var spawnedObject = Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation);
            networkGameObjects.Add(guid, spawnedObject);

            var setToLocalClient = (Action<GameObject>)(
                instanceId == playerObject.GetInstanceID()
                    ? connectedClients[participant].SetPlayerObject
                    : connectedClients[participant].AddNetworkObject
            );
            setToLocalClient(spawnedObject);
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

        public async UniTask ConnectAsync(string token, string url = default)
        {
            if (transport.IsConnected)
            {
                if (Logger.IsWarn())
                {
                    Logger.LogWarn("This client is already connected.");
                }
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                url = relayURL;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: url={url}, token={token}");
            }

            var participant = await transport.ConnectAsync(url, token);
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

        public void DeleteRoom()
            => transport.DeleteRoom();

        public GameObject SpawnPlayer(Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            if (playerObject == null)
            {
                throw new InvalidOperationException("Add an object to use as player to the playerObject of this instance");
            }

            return SpawnInternal(playerObject, position, rotation, parent, true);
        }

        public GameObject SpawnObject(GameObject objectPrefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            if (!networkObjectPrefabs.ContainsKey(objectPrefab.GetInstanceID()))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects you have added to the networkObjects of this instance");
            }

            return SpawnInternal(objectPrefab, position, rotation, parent, false);
        }

        public GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool isPlayer)
        {
            var networkObjectInfo = new NetworkObjectInfo(Guid.NewGuid(), prefab.GetInstanceID(), position, rotation);
            var localObject = Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation, parent);

            var setToLocalClient = (Action<GameObject>)(isPlayer ? LocalClient.SetPlayerObject : LocalClient.AddNetworkObject);
            setToLocalClient(localObject);

            localNetworkObjectInfos.Add(networkObjectInfo.ObjectGuid, networkObjectInfo);
            networkGameObjects.Add(networkObjectInfo.ObjectGuid, localObject);

            return localObject;
        }

        public void SendMessage(string messageName, string messageJson, DataPacketKind dataPacketKind = DataPacketKind.RELIABLE)
        {
            var message = JsonUtility.ToJson(new LiveKidMultiplayMessageContainer(messageName, messageJson));
            transport.SendMessageAsync(message, dataPacketKind).Forget();
        }

        public async UniTask<Room[]> ListRooms() { return Array.Empty<Room>(); }
    }

#if !UNITY_WEBGL || UNITY_EDITOR
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
