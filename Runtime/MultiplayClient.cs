using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using Extreal.Integration.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging
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
        /// Joined users.
        /// <para>Key: User ID.</para>
        /// <para>Value: Network client.</para>
        /// </summary>
        public IReadOnlyDictionary<string, NetworkClient> JoinedUsers => joinedUsers;
        private readonly Dictionary<string, NetworkClient> joinedUsers = new Dictionary<string, NetworkClient>();

        /// <summary>
        /// <para>Invokes immediately after this client joins a group.</para>
        /// Arg: User ID of this client.
        /// </summary>
        public IObservable<string> OnJoined => MessagingClient.OnJoined;

        /// <summary>
        /// <para>Invokes just before this client leaves a group.</para>
        /// Arg: reason why this client leaves.
        /// </summary>
        public IObservable<string> OnLeaving => MessagingClient.OnLeaving;

        /// <summary>
        /// <para>Invokes immediately after this client unexpectedly leaves a group.</para>
        /// Arg: reason why this client leaves.
        /// </summary>
        public IObservable<string> OnUnexpectedLeft => MessagingClient.OnUnexpectedLeft;

        /// <summary>
        /// Invokes immediately after the joining approval is rejected.
        /// </summary>
        public IObservable<Unit> OnJoiningApprovalRejected => MessagingClient.OnJoiningApprovalRejected;

        /// <summary>
        /// <para>Invokes immediately after a user joins the group this client joined.</para>
        /// Arg: ID of the joined user.
        /// </summary>
        public IObservable<string> OnUserJoined => OnUserJoinedProtected;
        [SuppressMessage("Usage", "CC0033")]
        protected Subject<string> OnUserJoinedProtected { get; } = new Subject<string>();

        /// <summary>
        /// <para>Invokes just before a user leaves the group this client joined.</para>
        /// Arg: ID of the left user.
        /// </summary>
        public IObservable<string> OnUserLeaving => MessagingClient.OnUserLeaving;

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
        protected Dictionary<Guid, NetworkObjectInfo> RemoteNetworkObjectInfos { get; } = new Dictionary<Guid, NetworkObjectInfo>();
        protected Dictionary<Guid, GameObject> NetworkGameObjects { get; } = new Dictionary<Guid, GameObject>();
        protected Dictionary<int, GameObject> NetworkObjectPrefabs { get; } = new Dictionary<int, GameObject>();

        protected QueuingMessagingClient MessagingClient { get; }

        private bool isDisposed;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(MultiplayClient));

        /// <summary>
        /// Creates a new MultiplayClient.
        /// </summary>
        /// <param name="messagingClient">QueuingMessagingClient.</param>
        /// <param name="networkObjectsProvider">NetworkObjectsProvider that implements INetworkObjectsProvider.</param>
        /// <exception cref="ArgumentNullException">When messagingClient or networkObjectsProvider is null.</exception>
        public MultiplayClient(QueuingMessagingClient messagingClient, INetworkObjectsProvider networkObjectsProvider)
        {
            if (messagingClient == null)
            {
                throw new ArgumentNullException(nameof(messagingClient));
            }
            if (networkObjectsProvider == null)
            {
                throw new ArgumentNullException(nameof(networkObjectsProvider));
            }

            onObjectSpawned.AddTo(disposables);
            onMessageReceived.AddTo(disposables);
            MessagingClient = messagingClient.AddTo(disposables);

            networkObjectsProvider.Provide().ForEach(AddToNetworkObjectPrefabs);

            UpdateAsync().Forget();

            MessagingClient.OnJoined
                .Subscribe(userId =>
                {
                    LocalClient = new NetworkClient(userId);
                    joinedUsers[userId] = LocalClient;
                })
                .AddTo(disposables);

            MessagingClient.OnLeaving
                .Merge(MessagingClient.OnUnexpectedLeft)
                .Subscribe(_ => Clear())
                .AddTo(disposables);

            MessagingClient.OnUserJoined
                .Subscribe(connectedUserId =>
                {
                    if (!joinedUsers.ContainsKey(connectedUserId))
                    {
                        joinedUsers[connectedUserId] = new NetworkClient(connectedUserId);
                    }

                    var networkObjectInfos = localNetworkObjectInfos.Values.ToArray();
                    var message = new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjectInfos: networkObjectInfos).ToJson();
                    MessagingClient.EnqueueRequest(message, connectedUserId);
                })
                .AddTo(disposables);

            MessagingClient.OnUserLeaving
                .Subscribe(disconnectingUserId =>
                {
                    if (joinedUsers.TryGetValue(disconnectingUserId, out var networkClient))
                    {
                        foreach (var networkObject in networkClient.NetworkObjects)
                        {
                            var objectGuid = NetworkGameObjects.First(pair => pair.Value == networkObject).Key;
                            NetworkGameObjects.Remove(objectGuid);
                            RemoteNetworkObjectInfos.Remove(objectGuid);
                            UnityEngine.Object.Destroy(networkObject);
                        }
                        joinedUsers.Remove(disconnectingUserId);
                    }
                })
                .AddTo(disposables);
        }

        private void AddToNetworkObjectPrefabs(GameObject go)
        {
            var selfInstanceId = MultiplayUtil.GetGameObjectHash(go);
            NetworkObjectPrefabs.Add(selfInstanceId, go);
        }

        protected override void ReleaseManagedResources()
        {
            Clear();
            disposables.Dispose();
            isDisposed = true;
        }

        private void Clear()
        {
            foreach (var networkGameObject in NetworkGameObjects.Values)
            {
                UnityEngine.Object.Destroy(networkGameObject);
            }

            LocalClient = null;
            joinedUsers.Clear();
            localNetworkObjectInfos.Clear();
            NetworkGameObjects.Clear();
        }

        private async UniTaskVoid UpdateAsync()
        {
            while (!isDisposed)
            {
                await UniTask.Yield();
                if (!MessagingClient.IsJoinedGroup)
                {
                    continue;
                }

                SynchronizeToOthers();
                SynchronizeLocal();
            }
        }

        private void SynchronizeToOthers()
        {
            var networkObjectInfosToSend = new List<NetworkObjectInfo>();
            foreach ((var guid, var networkObjectInfo) in localNetworkObjectInfos)
            {
                var localGameObject = NetworkGameObjects[guid];
                if (localGameObject.TryGetComponent(out PlayerInput input))
                {
                    networkObjectInfo.GetValuesFrom(in input);
                }

                if (networkObjectInfo.CheckWhetherToSendData())
                {
                    networkObjectInfo.GetTransformFrom(localGameObject.transform);
                    networkObjectInfosToSend.Add(networkObjectInfo);
                }
            }
            if (networkObjectInfosToSend.Count > 0)
            {
                var message = new MultiplayMessage(MultiplayMessageCommand.Update, networkObjectInfos: networkObjectInfosToSend.ToArray()).ToJson();
                MessagingClient.EnqueueRequest(message);
            }
        }

        protected virtual void SynchronizeLocal()
        {
            while (MessagingClient.ResponseQueueCount() > 0)
            {
                (var from, var messageJson) = MessagingClient.DequeueResponse();
                var message = MultiplayMessage.FromJson(messageJson);

                if (message.Command is MultiplayMessageCommand.Create)
                {
                    if (!joinedUsers.ContainsKey(from))
                    {
                        joinedUsers[from] = new NetworkClient(from);
                    }
                    CreateObject(from, message.NetworkObjectInfo, message.Message);
                }
                else if (message.Command is MultiplayMessageCommand.Update)
                {
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        UpdateNetworkObjectInfo(networkObjectInfo);
                    }
                }
                else if (message.Command is MultiplayMessageCommand.CreateExistedObject)
                {
                    if (!joinedUsers.ContainsKey(from))
                    {
                        joinedUsers[from] = new NetworkClient(from);
                    }
                    foreach (var networkObjectInfo in message.NetworkObjectInfos)
                    {
                        CreateObject(from, networkObjectInfo);
                    }
                    var responseMsg = new MultiplayMessage(MultiplayMessageCommand.UserInitialized).ToJson();
                    MessagingClient.EnqueueRequest(responseMsg, from);
                }
                else if (message.Command is MultiplayMessageCommand.UserInitialized)
                {
                    OnUserJoinedProtected.OnNext(from);
                }
                else if (message.Command is MultiplayMessageCommand.Message)
                {
                    onMessageReceived.OnNext((from, message.Message));
                }
            }

            foreach (var networkObjectInfo in RemoteNetworkObjectInfos.Values)
            {
                UpdateObjectTransform(networkObjectInfo);
            }
        }

        protected void CreateObject(string userId, NetworkObjectInfo networkObjectInfo, string message = default)
        {
            if (RemoteNetworkObjectInfos.ContainsKey(networkObjectInfo.ObjectGuid))
            {
                return;
            }

            RemoteNetworkObjectInfos[networkObjectInfo.ObjectGuid] = networkObjectInfo;

            var gameObjectHash = networkObjectInfo.GameObjectHash;
            if (Logger.IsDebug())
            {
                Logger.LogDebug(
                    "Create network object:"
                    + $" userId={userId}, ObjectGuid={networkObjectInfo.ObjectGuid}, gameObjectHash={gameObjectHash}");
            }

            SpawnInternal(NetworkObjectPrefabs[gameObjectHash], networkObjectInfo, joinedUsers[userId].AddNetworkObject, userId, message: message);
        }

        protected void UpdateNetworkObjectInfo(NetworkObjectInfo networkObjectInfo)
        {
            if (NetworkGameObjects.TryGetValue(networkObjectInfo.ObjectGuid, out var objectToBeUpdated))
            {
                networkObjectInfo.SetPreTransform(objectToBeUpdated.transform);
                RemoteNetworkObjectInfos[networkObjectInfo.ObjectGuid] = networkObjectInfo;

                if (objectToBeUpdated.TryGetComponent(out PlayerInput input))
                {
                    networkObjectInfo.ApplyValuesTo(in input);
                }
            }
        }

        protected void UpdateObjectTransform(NetworkObjectInfo networkObjectInfo)
        {
            if (NetworkGameObjects.TryGetValue(networkObjectInfo.ObjectGuid, out var objectToBeUpdated))
            {
                networkObjectInfo.SetTransformTo(objectToBeUpdated.transform);
            }
        }

        /// <summary>
        /// Joins a group.
        /// </summary>
        /// <param name="joiningConfig">Joining Config.</param>
        /// <exception cref="ArgumentNullException">When joiningConfig is null.</exception>
        public UniTask JoinAsync(MultiplayJoiningConfig joiningConfig)
        {
            if (joiningConfig == null)
            {
                throw new ArgumentNullException(nameof(joiningConfig));
            }

            return MessagingClient.JoinAsync(joiningConfig.MessagingJoiningConfig);
        }

        /// <summary>
        /// Leaves a group.
        /// </summary>
        public UniTask LeaveAsync()
            => MessagingClient.LeaveAsync();

        /// <summary>
        /// Spawns an object.
        /// </summary>
        /// <param name="objectPrefab">Prefab of the object to be spawned.</param>
        /// <param name="position">Initial position of the object when it is spawned.</param>
        /// <param name="rotation">Initial rotation of the object when it is spawned.</param>
        /// <param name="parent">Parent to be set to the object.</param>
        /// <param name="message">Message to be publish with spawned object when the object is spawned.</param>
        /// <exception cref="ArgumentNullException">When objectPrefab is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When not specified any of the objects that INetworkObjectsProvider provides.</exception>
        /// <returns>Spawned object.</returns>
        public GameObject SpawnObject(GameObject objectPrefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default, string message = default)
        {
            if (objectPrefab == null)
            {
                throw new ArgumentNullException(nameof(objectPrefab));
            }

            var gameObjectHash = MultiplayUtil.GetGameObjectHash(objectPrefab);
            if (!NetworkObjectPrefabs.ContainsKey(gameObjectHash))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects that INetworkObjectsProvider provides");
            }

            if (rotation.x == default)
            {
                rotation = Quaternion.identity;
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
            NetworkGameObjects.Add(networkObjectInfo.ObjectGuid, spawnedObject);
            if (userId == LocalClient?.UserId)
            {
                localNetworkObjectInfos.Add(networkObjectInfo.ObjectGuid, networkObjectInfo);
                var messageJson = new MultiplayMessage(MultiplayMessageCommand.Create, networkObjectInfo: networkObjectInfo, message: message).ToJson();
                MessagingClient.EnqueueRequest(messageJson);
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
        /// <exception cref="ArgumentNullException">When message is null.</exception>
        public void SendMessage(string message, string to = default)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageJson = new MultiplayMessage(MultiplayMessageCommand.Message, message: message).ToJson();
            MessagingClient.EnqueueRequest(messageJson, to);
        }
    }
}
