using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
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
    public class MultiplayClient : DisposableBase
    {
        /// <summary>
        /// Local client.
        /// </summary>
        public NetworkClient LocalClient { get; private set; }

        /// <summary>
        /// Connected users.
        /// <para>Key: User ID.</para>
        /// <para>Value: Network client.</para>
        /// </summary>
        public IReadOnlyDictionary<string, NetworkClient> JoinedUsers => joinedUsers;
        private readonly Dictionary<string, NetworkClient> joinedUsers = new Dictionary<string, NetworkClient>();

        /// <summary>
        /// <para>Invokes immediately after this client connects to a group.</para>
        /// Arg: User ID of this client.
        /// </summary>
        public IObservable<string> OnJoined => messagingClient.OnJoined;

        /// <summary>
        /// <para>Invokes just before this client disconnects from a group.</para>
        /// Arg: reason why this client disconnects.
        /// </summary>
        public IObservable<string> OnLeaving => messagingClient.OnLeaving;

        /// <summary>
        /// <para>Invokes immediately after this client unexpectedly disconnects from the server.</para>
        /// Arg: reason why this client disconnects.
        /// </summary>
        public IObservable<string> OnUnexpectedLeft => messagingClient.OnUnexpectedLeft;

        /// <summary>
        /// Invokes immediately after the connection approval is rejected.
        /// </summary>
        public IObservable<Unit> OnJoiningApprovalRejected => messagingClient.OnJoiningApprovalRejected;

        /// <summary>
        /// <para>Invokes immediately after a user connects to a group.</para>
        /// Arg: ID of the connected user.
        /// </summary>
        public IObservable<string> OnUserJoined => onUserJoined;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onUserJoined = new Subject<string>();

        /// <summary>
        /// <para>Invokes just before a user disconnects from a group.</para>
        /// Arg: ID of the disconnected user.
        /// </summary>
        public IObservable<string> OnUserLeaving => messagingClient.OnUserLeaving;

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

        private readonly QueuingMessagingClient messagingClient;

        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(MultiplayClient));

        /// <summary>
        /// Creates a new MultiplayClient.
        /// </summary>
        /// <param name="messagingClient">QueuingMessagingClient.</param>
        /// <exception cref="ArgumentNullException">When messagingClient is null.</exception>
        public MultiplayClient(QueuingMessagingClient messagingClient, INetworkObjectsProvider networkObjectsProvider)
        {
            if (messagingClient == null)
            {
                throw new ArgumentNullException(nameof(messagingClient));
            }

            onObjectSpawned.AddTo(disposables);
            onMessageReceived.AddTo(disposables);
            this.messagingClient = messagingClient.AddTo(disposables);

            networkObjectsProvider.Provide().ForEach(AddToNetworkObjectPrefabs);

            Observable.EveryUpdate()
                .Subscribe(_ => Update())
                .AddTo(disposables);

            this.messagingClient.OnJoined
                .Subscribe(userId =>
                {
                    LocalClient = new NetworkClient(userId);
                    joinedUsers[userId] = LocalClient;
                })
                .AddTo(disposables);

            this.messagingClient.OnLeaving
                .Merge(this.messagingClient.OnUnexpectedLeft)
                .Subscribe(_ => Clear())
                .AddTo(disposables);

            this.messagingClient.OnUserJoined
                .Subscribe(connectedUserId =>
                {
                    joinedUsers[connectedUserId] = new NetworkClient(connectedUserId);

                    var networkObjectInfos = localNetworkObjectInfos.Values.ToArray();
                    var message = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjectInfos: networkObjectInfos));
                    this.messagingClient.EnqueueRequest(message, connectedUserId);
                })
                .AddTo(disposables);

            this.messagingClient.OnUserLeaving
                .Subscribe(disconnectingUserId =>
                {
                    if (joinedUsers.TryGetValue(disconnectingUserId, out var networkClient))
                    {
                        foreach (var networkObject in networkClient.NetworkObjects)
                        {
                            UnityEngine.Object.Destroy(networkObject);
                        }
                        joinedUsers.Remove(disconnectingUserId);
                    }
                })
                .AddTo(disposables);
        }

        private void AddToNetworkObjectPrefabs(GameObject go)
        {
            var selfInstanceId = GetGameObjectHash(go);
            networkObjectPrefabs.Add(selfInstanceId, go);
        }

        protected override void ReleaseManagedResources()
        {
            Clear();
            disposables.Dispose();
        }

        private void Clear()
        {
            foreach (var networkGameObject in networkGameObjects.Values)
            {
                UnityEngine.Object.Destroy(networkGameObject);
            }

            LocalClient = null;
            joinedUsers.Clear();
            localNetworkObjectInfos.Clear();
            networkGameObjects.Clear();
        }

        private void Update()
        {
            if (!messagingClient.IsJoinedGroup)
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

                if (localGameObject.TryGetComponent(out PlayerInput input))
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
                    joinedUsers[from] = new NetworkClient(from);
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        CreateObject(from, networkObjectInfo);
                    }
                    var responseMsg = JsonUtility.ToJson(new MultiplayMessage(MultiplayMessageCommand.UserInitialized));
                    messagingClient.EnqueueRequest(responseMsg, from);
                }
                else if (message.Command is MultiplayMessageCommand.UserInitialized)
                {
                    onUserJoined.OnNext(from);
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

            SpawnInternal(networkObjectPrefabs[gameObjectHash], networkObjectInfo, joinedUsers[userId].AddNetworkObject, userId, message: message);
        }

        private void UpdateObject(NetworkObjectInfo networkObjectInfo)
        {
            if (networkGameObjects.TryGetValue(networkObjectInfo.ObjectGuid, out var objectToBeUpdated))
            {
                if (objectToBeUpdated.TryGetComponent(out PlayerInput input))
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
        public UniTask JoinAsync(MultiplayJoiningConfig connectionConfig)
            => messagingClient.JoinAsync(connectionConfig.MessagingJoiningConfig);

        /// <summary>
        /// Disconnects from a group.
        /// </summary>
        public UniTask LeaveAsync()
            => messagingClient.LeaveAsync();

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
            var spawnedObject = UnityEngine.Object.Instantiate(prefab, networkObjectInfo.Position, networkObjectInfo.Rotation, parent);
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
    }
}
