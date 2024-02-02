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
        /// Joined clients.
        /// <para>Key: Client ID.</para>
        /// <para>Value: Network client.</para>
        /// </summary>
        public IReadOnlyDictionary<string, NetworkClient> JoinedClients => JoinedClientsProtected;
        protected Dictionary<string, NetworkClient> JoinedClientsProtected { get; } = new Dictionary<string, NetworkClient>();

        /// <summary>
        /// <para>Invokes immediately after this client joins a group.</para>
        /// Arg: Client ID of this client.
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
        /// <para>Invokes immediately after a client joins the group this client joined.</para>
        /// Arg: ID of the joined client.
        /// </summary>
        public IObservable<string> OnClientJoined => OnClientJoinedProtected;
        [SuppressMessage("Usage", "CC0033")]
        protected Subject<string> OnClientJoinedProtected { get; } = new Subject<string>();

        /// <summary>
        /// <para>Invokes just before a client leaves the group this client joined.</para>
        /// Arg: ID of the left client.
        /// </summary>
        public IObservable<string> OnClientLeaving => MessagingClient.OnClientLeaving;

        /// <summary>
        /// <para>Invokes immediately after an object is spawned.</para>
        /// <para>Arg1: ID of the client that spawns this object.</para>
        /// <para>Arg2: Spawned object.</para>
        /// <para>Arg3: Message added to the spawn of this object. Null if not added.</para>
        /// </summary>
        public IObservable<(string clientId, GameObject spawnedObject, string message)> OnObjectSpawned => onObjectSpawned;
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, GameObject, string)> onObjectSpawned = new Subject<(string, GameObject, string)>();

        /// <summary>
        /// <para>Invokes immediately after the message is received.</para>
        /// Arg: ID of the client sending the message and the message.
        /// </summary>
        public IObservable<(string from, string message)> OnMessageReceived => OnMessageReceivedProtected;
        [SuppressMessage("Usage", "CC0033")]
        protected Subject<(string, string)> OnMessageReceivedProtected { get; } = new Subject<(string, string)>();

        protected Dictionary<Guid, NetworkObject> LocalNetworkObjects { get; } = new Dictionary<Guid, NetworkObject>();
        protected Dictionary<Guid, NetworkObject> RemoteNetworkObjects { get; } = new Dictionary<Guid, NetworkObject>();
        protected Dictionary<Guid, GameObject> NetworkGameObjects { get; } = new Dictionary<Guid, GameObject>();
        private readonly Dictionary<string, GameObject> networkObjectPrefabs = new Dictionary<string, GameObject>();

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
            OnMessageReceivedProtected.AddTo(disposables);
            MessagingClient = messagingClient.AddTo(disposables);

            networkObjectPrefabs = networkObjectsProvider.Provide();

            UpdateAsync().Forget();

            MessagingClient.OnJoined
                .Subscribe(clientId =>
                {
                    LocalClient = new NetworkClient(clientId);
                    JoinedClientsProtected[clientId] = LocalClient;
                })
                .AddTo(disposables);

            MessagingClient.OnLeaving
                .Merge(MessagingClient.OnUnexpectedLeft)
                .Subscribe(_ => Clear())
                .AddTo(disposables);

            MessagingClient.OnClientJoined
                .Subscribe(joinedClientId =>
                {
                    if (!JoinedClientsProtected.ContainsKey(joinedClientId))
                    {
                        JoinedClientsProtected[joinedClientId] = new NetworkClient(joinedClientId);
                    }

                    var networkObjects = LocalNetworkObjects.Values.ToArray();
                    var multiplayMessage = new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjects: networkObjects);
                    MessagingClient.EnqueueRequest(multiplayMessage.ToJson(), joinedClientId);
                })
                .AddTo(disposables);

            MessagingClient.OnClientLeaving
                .Subscribe(leavingClientId =>
                {
                    if (JoinedClientsProtected.TryGetValue(leavingClientId, out var networkClient))
                    {
                        foreach (var networkObject in networkClient.NetworkObjects)
                        {
                            var objectGuid = NetworkGameObjects.First(pair => pair.Value == networkObject).Key;
                            NetworkGameObjects.Remove(objectGuid);
                            RemoteNetworkObjects.Remove(objectGuid);
                            UnityEngine.Object.Destroy(networkObject);
                        }
                        JoinedClientsProtected.Remove(leavingClientId);
                    }
                })
                .AddTo(disposables);
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
            JoinedClientsProtected.Clear();
            LocalNetworkObjects.Clear();
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

        protected virtual void SynchronizeToOthers()
        {
            var networkObjectsToSend = new List<NetworkObject>();
            foreach ((var guid, var networkObject) in LocalNetworkObjects)
            {
                var localGameObject = NetworkGameObjects[guid];
                if (localGameObject.TryGetComponent(out PlayerInput input))
                {
                    networkObject.GetValuesFrom(in input);
                }

                if (networkObject.CheckWhetherToSendData())
                {
                    networkObject.GetTransformFrom(localGameObject.transform);
                    networkObjectsToSend.Add(networkObject);
                }
            }
            if (networkObjectsToSend.Count > 0)
            {
                var multiplayMessage = new MultiplayMessage(MultiplayMessageCommand.Update, networkObjects: networkObjectsToSend.ToArray());
                MessagingClient.EnqueueRequest(multiplayMessage.ToJson());
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
                    if (!JoinedClientsProtected.ContainsKey(from))
                    {
                        JoinedClientsProtected[from] = new NetworkClient(from);
                    }
                    CreateObject(from, message.NetworkObject, message.Message);
                }
                else if (message.Command is MultiplayMessageCommand.Update)
                {
                    foreach (var networkObject in message.NetworkObjects)
                    {
                        UpdateNetworkObject(networkObject);
                    }
                }
                else if (message.Command is MultiplayMessageCommand.CreateExistedObject)
                {
                    if (!JoinedClientsProtected.ContainsKey(from))
                    {
                        JoinedClientsProtected[from] = new NetworkClient(from);
                    }
                    foreach (var networkObject in message.NetworkObjects)
                    {
                        CreateObject(from, networkObject);
                    }
                    var responseMsg = new MultiplayMessage(MultiplayMessageCommand.ClientInitialized);
                    MessagingClient.EnqueueRequest(responseMsg.ToJson(), from);
                }
                else if (message.Command is MultiplayMessageCommand.ClientInitialized)
                {
                    OnClientJoinedProtected.OnNext(from);
                }
                else if (message.Command is MultiplayMessageCommand.Message)
                {
                    OnMessageReceivedProtected.OnNext((from, message.Message));
                }
            }

            foreach (var networkObject in RemoteNetworkObjects.Values)
            {
                UpdateObjectTransform(networkObject);
            }
        }

        protected void CreateObject(string clientId, NetworkObject networkObject, string message = default)
        {
            if (RemoteNetworkObjects.ContainsKey(networkObject.ObjectGuid))
            {
                // Not covered by testing due to defensive implementation
                return;
            }
            RemoteNetworkObjects[networkObject.ObjectGuid] = networkObject;

            var gameObjectKey = networkObject.GameObjectKey;
            if (Logger.IsDebug())
            {
                Logger.LogDebug(
                    "Create network object:"
                    + $" clientId={clientId}, ObjectGuid={networkObject.ObjectGuid}, gameObjectKey={gameObjectKey}");
            }

            SpawnInternal(networkObjectPrefabs[gameObjectKey], networkObject, JoinedClientsProtected[clientId].AddNetworkObject, clientId, message: message);
        }

        protected void UpdateNetworkObject(NetworkObject networkObject)
        {
            if (NetworkGameObjects.TryGetValue(networkObject.ObjectGuid, out var objectToBeUpdated))
            {
                networkObject.SetPreTransform(objectToBeUpdated.transform);
                RemoteNetworkObjects[networkObject.ObjectGuid] = networkObject;

                if (objectToBeUpdated.TryGetComponent(out PlayerInput input))
                {
                    networkObject.ApplyValuesTo(in input);
                }
            }
        }

        protected void UpdateObjectTransform(NetworkObject networkObject)
        {
            if (NetworkGameObjects.TryGetValue(networkObject.ObjectGuid, out var objectToBeUpdated))
            {
                networkObject.SetTransformTo(objectToBeUpdated.transform);
            }
        }

        /// <summary>
        /// Lists groups that currently exist.
        /// </summary>
        /// <returns>List of the groups that currently exist.</returns>
        public async UniTask<List<Group>> ListGroupsAsync() => await MessagingClient.ListGroupsAsync();

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

            if (!networkObjectPrefabs.ContainsValue(objectPrefab))
            {
                throw new ArgumentOutOfRangeException(nameof(objectPrefab), "Specify any of the objects that INetworkObjectsProvider provides");
            }
            var gameObjectKey = networkObjectPrefabs.First(x => x.Value == objectPrefab).Key;

            if (rotation.x == 0f && rotation.y == 0f && rotation.z == 0f && rotation.w == 0f)
            {
                rotation = Quaternion.identity;
            }
            var networkObject = new NetworkObject(gameObjectKey, position, rotation);
            return SpawnInternal(objectPrefab, networkObject, LocalClient.AddNetworkObject, LocalClient.ClientId, parent, message);
        }

        private GameObject SpawnInternal
        (
            GameObject prefab,
            NetworkObject networkObject,
            Action<GameObject> setToNetworkClient,
            string clientId,
            Transform parent = default,
            string message = default
        )
        {
            var spawnedObject = UnityEngine.Object.Instantiate(prefab, networkObject.Position, networkObject.Rotation, parent);
            setToNetworkClient.Invoke(spawnedObject);
            NetworkGameObjects.Add(networkObject.ObjectGuid, spawnedObject);
            if (clientId == LocalClient?.ClientId)
            {
                LocalNetworkObjects.Add(networkObject.ObjectGuid, networkObject);
                var multiplayMessage = new MultiplayMessage(MultiplayMessageCommand.Create, networkObject: networkObject, message: message);
                MessagingClient.EnqueueRequest(multiplayMessage.ToJson());
            }

            onObjectSpawned.OnNext((clientId, spawnedObject, message));
            return spawnedObject;
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        /// <param name="to">
        ///     Client ID of the destination.
        ///     <para>Sends a message to the entire group if not specified.</para>
        /// </param>
        /// <exception cref="ArgumentNullException">When message is null.</exception>
        public void SendMessage(string message, string to = default)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var multiplayMessage = new MultiplayMessage(MultiplayMessageCommand.Message, message: message);
            MessagingClient.EnqueueRequest(multiplayMessage.ToJson(), to);
        }
    }
}
