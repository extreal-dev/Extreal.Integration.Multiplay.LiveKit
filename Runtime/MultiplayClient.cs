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
    /// <summary>
    /// Class for group multiplayer.
    /// </summary>
    public class MultiplayClient : MonoBehaviour
    {
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject[] networkObjects;

        /// <summary>
        /// Local client.
        /// </summary>
        public NetworkClient LocalClient { get; private set; }

        /// <summary>
        /// Connected users.
        /// <para>Key: User ID.</para>
        /// <para>Value: Network client.</para>
        /// </summary>
        public IReadOnlyDictionary<string, NetworkClient> ConnectedUsers => connectedUsers;
        private readonly Dictionary<string, NetworkClient> connectedUsers = new Dictionary<string, NetworkClient>();

        /// <summary>
        /// <para>Invokes immediately after this client connects to a group.</para>
        /// Arg: User ID of this client.
        /// </summary>
        public IObservable<string> OnConnected => messagingClient.OnConnected;

        /// <summary>
        /// <para>Invokes just before this client disconnects from a group.</para>
        /// Arg: reason why this client disconnects.
        /// </summary>
        public IObservable<string> OnDisconnecting => messagingClient.OnDisconnecting;

        /// <summary>
        /// <para>Invokes immediately after this client unexpectedly disconnects from the server.</para>
        /// Arg: reason why this client disconnects.
        /// </summary>
        public IObservable<string> OnUnexpectedDisconnected => messagingClient.OnUnexpectedDisconnected;

        /// <summary>
        /// Invokes immediately after the connection approval is rejected.
        /// </summary>
        public IObservable<Unit> OnConnectionApprovalRejected => messagingClient.OnConnectionApprovalRejected;

        /// <summary>
        /// <para>Invokes immediately after a user connects to a group.</para>
        /// Arg: ID of the connected user.
        /// </summary>
        public IObservable<string> OnUserConnected => onUserConnected;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onUserConnected = new Subject<string>();

        /// <summary>
        /// <para>Invokes just before a user disconnects from a group.</para>
        /// Arg: ID of the disconnected user.
        /// </summary>
        public IObservable<string> OnUserDisconnecting => messagingClient.OnUserDisconnecting;

        /// <summary>
        /// <para>Invokes immediately after an object is spawned.</para>
        /// <para>Arg1: ID of the user that spawns this object.</para>
        /// <para>Arg2: Spawned object.</para>
        /// <para>Arg3: Message added to the spawn of this object. Null if not added.</para>
        /// </summary>
        public IObservable<(string userId, GameObject spawnedObject, string message)> OnObjectSpawned => onObjectSpawned;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, GameObject, string)> onObjectSpawned = new Subject<(string, GameObject, string)>();

        /// <summary>
        /// <para>Invokes immediately after the message is received.</para>
        /// Arg: ID of the user sending the message and the message.
        /// </summary>
        public IObservable<(string from, string message)> OnMessageReceived => onMessageReceived;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, string)> onMessageReceived = new Subject<(string, string)>();

        private readonly Dictionary<Guid, NetworkObjectInfo> localNetworkObjectInfos = new Dictionary<Guid, NetworkObjectInfo>();
        private readonly Dictionary<Guid, GameObject> networkGameObjects = new Dictionary<Guid, GameObject>();
        private readonly Dictionary<int, GameObject> networkObjectPrefabs = new Dictionary<int, GameObject>();

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

        /// <summary>
        /// Sets a messaging client.
        /// </summary>
        /// <param name="messagingClient">QueuingMessagingClient</param>
        /// <exception cref="ArgumentNullException">When messagingClient is null.</exception>
        public void SetMessagingClient(QueuingMessagingClient messagingClient)
        {
            if (messagingClient == null)
            {
                throw new ArgumentNullException(nameof(messagingClient));
            }

            this.messagingClient = messagingClient.AddTo(this);

            this.messagingClient.OnConnected
                .TakeUntilDestroy(this)
                .Subscribe(userId =>
                {
                    LocalClient = new NetworkClient(userId);
                    connectedUsers[userId] = LocalClient;
                });

            this.messagingClient.OnDisconnecting
                .Merge(this.messagingClient.OnUnexpectedDisconnected)
                .TakeUntilDestroy(this)
                .Subscribe(_ => Clear());

            this.messagingClient.OnUserConnected
                .TakeUntilDestroy(this)
                .Subscribe(connectedUserId =>
                {
                    connectedUsers[connectedUserId] = new NetworkClient(connectedUserId);

                    var networkObjectInfos = localNetworkObjectInfos.Values.ToArray();
                    var message = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjectInfos: networkObjectInfos));
                    this.messagingClient.EnqueueRequest(message, connectedUserId);
                });

            this.messagingClient.OnUserDisconnecting
                .TakeUntilDestroy(this)
                .Subscribe(disconnectingUserId =>
                {
                    if (connectedUsers.TryGetValue(disconnectingUserId, out var networkClient))
                    {
                        if (networkClient.PlayerObject != null)
                        {
                            Destroy(networkClient.PlayerObject);
                        }
                        foreach (var networkObject in networkClient.NetworkObjects)
                        {
                            Destroy(networkObject);
                        }
                        connectedUsers.Remove(disconnectingUserId);
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
            connectedUsers.Clear();
            localNetworkObjectInfos.Clear();
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
            foreach ((var guid, var networkObjectInfo) in localNetworkObjectInfos)
            {
                var localGameObject = networkGameObjects[guid];
                networkObjectInfo.GetTransformFrom(localGameObject.transform);

                if (localGameObject.TryGetComponent(out MultiplayPlayerInput input))
                {
                    networkObjectInfo.GetValuesFrom(in input);
                }

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
                if (localNetworkObjectInfos.ContainsKey(message.NetworkObjectInfo.ObjectGuid))
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
                    connectedUsers[from] = new NetworkClient(from);
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
                    + $" userId={userId}, ObjectGuid={networkObjectInfo.ObjectGuid}, gameObjectHash={gameObjectHash}");
            }

            var prefab = networkObjectPrefabs[gameObjectHash];
            var setToNetworkClient = (Action<GameObject>)(
                gameObjectHash == GetGameObjectHash(playerObject)
                    ? connectedUsers[userId].SetPlayerObject
                    : connectedUsers[userId].AddNetworkObject
            );

            SpawnInternal(prefab, networkObjectInfo, setToNetworkClient, userId, message: message);
        }

        private void UpdateObject(NetworkObjectInfo networkObjectInfo)
        {
            if (networkGameObjects.TryGetValue(networkObjectInfo.ObjectGuid, out var objectToBeUpdated))
            {
                if (objectToBeUpdated.TryGetComponent(out MultiplayPlayerInput input))
                {
                    networkObjectInfo.ApplyValuesTo(in input);
                }

                if ((objectToBeUpdated.transform.position - networkObjectInfo.Position).sqrMagnitude > 0f)
                {
                    objectToBeUpdated.transform.position = networkObjectInfo.Position;
                    objectToBeUpdated.transform.rotation = networkObjectInfo.Rotation;
                }
            }
        }

        /// <summary>
        /// Connects to a group.
        /// </summary>
        /// <param name="connectionConfig">Connection Config.</param>
        public UniTask ConnectAsync(MessagingConnectionConfig connectionConfig)
        {
            CheckMessagingClient();
            return messagingClient.ConnectAsync(connectionConfig);
        }

        /// <summary>
        /// Disconnects from a group.
        /// </summary>
        public UniTask DisconnectAsync()
        {
            CheckMessagingClient();
            return messagingClient.DisconnectAsync();
        }

        /// <summary>
        /// Spawns a player object set to this instance.
        /// </summary>
        /// <param name="position">Initial position of the player object when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the player object when it is spawned.</param>
        /// <param name="parent">Parent to be set to the player object.</param>
        /// <param name="message">Message to be publish with spawned object when the player object is spawned.</param>
        /// <returns>Spawned object.</returns>
        public GameObject SpawnPlayer(Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            if (playerObject == null)
            {
                throw new InvalidOperationException("Add an object to use as player to the playerObject of this instance");
            }

            var selfGameObjectHash = GetGameObjectHash(playerObject);
            var networkObjectInfo = new NetworkObjectInfo(selfGameObjectHash, position, rotation);
            return SpawnInternal(playerObject, networkObjectInfo, LocalClient.SetPlayerObject, LocalClient.UserId, parent, message);
        }

        /// <summary>
        /// Spawns an object.
        /// </summary>
        /// <param name="objectPrefab">Prefab of the object to be spawned.</param>
        /// <param name="position">Initial position of the object when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the object when it is spawned.</param>
        /// <param name="parent">Parent to be set to the object.</param>
        /// <param name="message">Message to be publish with spawned object when the object is spawned.</param>
        /// <returns></returns>
        public GameObject SpawnObject(GameObject objectPrefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            if (objectPrefab == null)
            {
                throw new ArgumentNullException(nameof(objectPrefab));
            }

            var gameObjectHash = GetGameObjectHash(objectPrefab);
            if (!networkObjectPrefabs.ContainsKey(gameObjectHash))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects you have added to the networkObjects of this instance");
            }

            var networkObjectInfo = new NetworkObjectInfo(gameObjectHash, position, rotation);
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
            CheckMessagingClient();

            var spawnedObject = Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation, parent);
            setToNetworkClient.Invoke(spawnedObject);
            networkGameObjects.Add(networkObjectInfo.ObjectGuid, spawnedObject);
            if (userId == LocalClient?.UserId)
            {
                localNetworkObjectInfos.Add(networkObjectInfo.ObjectGuid, networkObjectInfo);
                var messageJson = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.Create, networkObjectInfo: networkObjectInfo, message: message));
                messagingClient.EnqueueRequest(messageJson);
            }

            onObjectSpawned.OnNext((userId, spawnedObject, message));
            return spawnedObject;
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        /// <param name="to">
        ///     User ID of the destination.
        ///     <para>Sends a message to the entire group if not specified.</para>
        /// </param>
        public void SendMessage(string message, string to = default)
        {
            CheckMessagingClient();

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageJson = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.Message, message: message));
            messagingClient.EnqueueRequest(messageJson, to);
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
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }
                var roots = scene.GetRootGameObjects();
                if (roots.Contains(target.transform.root.gameObject))
                {
                    return scene;
                }
            }
            return default;
        }

        private void CheckMessagingClient()
        {
            if (messagingClient == null)
            {
                throw new InvalidOperationException("Set Transport before this operation.");
            }
        }
    }
}
