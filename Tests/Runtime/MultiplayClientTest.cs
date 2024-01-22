using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.Messaging;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Extreal.Integration.Multiplay.Messaging.Test
{
    public class MultiplayClientTest
    {
        private MultiplayClient multiplayClient;
        private MockMessagingClient messagingClient;
        private NetworkObjectsProvider networkObjectsProvider;
        private readonly EventHandler eventHandler = new EventHandler();

        [SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            LoggingManager.Initialize(LogLevel.Debug);

            await SceneManager.LoadSceneAsync("Main");

            networkObjectsProvider = UnityEngine.Object.FindObjectOfType<NetworkObjectsProvider>();
            messagingClient = new MockMessagingClient();

            var queuingMessagingClient = new QueuingMessagingClient(messagingClient);
            multiplayClient = new MultiplayClient(queuingMessagingClient, networkObjectsProvider).AddTo(disposables);

            multiplayClient.OnJoined
                .Subscribe(eventHandler.SetClientId)
                .AddTo(disposables);

            multiplayClient.OnLeaving
                .Subscribe(eventHandler.SetLeavingReason)
                .AddTo(disposables);

            multiplayClient.OnUnexpectedLeft
                .Subscribe(eventHandler.SetUnexpectedLeftReason)
                .AddTo(disposables);

            multiplayClient.OnJoiningApprovalRejected
                .Subscribe(_ => eventHandler.SetIsJoiningApprovalRejected(true))
                .AddTo(disposables);

            multiplayClient.OnClientJoined
                .Subscribe(eventHandler.SetJoinedClientId)
                .AddTo(disposables);

            multiplayClient.OnClientLeaving
                .Subscribe(eventHandler.SetLeavingClientId)
                .AddTo(disposables);

            multiplayClient.OnMessageReceived
                .Subscribe(eventHandler.SetReceivedMessageInfo)
                .AddTo(disposables);

            multiplayClient.OnObjectSpawned
                .Subscribe(eventHandler.SetSpawnedObjectInfo)
                .AddTo(disposables);
        });

        [TearDown]
        public void Dispose()
        {
            eventHandler.Clear();
            disposables.Clear();
            multiplayClient = null;
            messagingClient = null;
            networkObjectsProvider = null;
        }

        [OneTimeTearDown]
        public void OneTimeDispose()
            => disposables.Dispose();

        [Test]
        public void NewMultiplayClientWithMessagingClientNull()
            => Assert.That(() => new MultiplayClient(null, networkObjectsProvider),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains(nameof(messagingClient)));

        [Test]
        public void NewMultiplayClientWithNetworkObjectsProviderClientNull()
            => Assert.That(() => new MultiplayClient(new QueuingMessagingClient(messagingClient), null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains(nameof(networkObjectsProvider)));

        [UnityTest]
        public IEnumerator ListGroupsSuccess() => UniTask.ToCoroutine(async () =>
        {
            var groups = await multiplayClient.ListGroupsAsync();
            Assert.That(groups, Is.Not.Null);
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Id, Is.EqualTo("TestId"));
            Assert.That(groups[0].Name, Is.EqualTo("TestName"));
        });

        [UnityTest]
        public IEnumerator JoinSuccess() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);

            Assert.That(eventHandler.ClientId, Is.Null);
            Assert.That(multiplayClient.LocalClient, Is.Null);
            Assert.That(multiplayClient.JoinedClients.Count, Is.Zero);

            await multiplayClient.JoinAsync(joiningConfig);

            Assert.That(eventHandler.ClientId, Is.Not.Null);
            Assert.That(multiplayClient.LocalClient, Is.Not.Null);
            Assert.That(multiplayClient.LocalClient.ClientId, Is.EqualTo(eventHandler.ClientId));
            Assert.That(multiplayClient.JoinedClients.Count, Is.EqualTo(1));
            Assert.That(multiplayClient.JoinedClients.ContainsKey(eventHandler.ClientId), Is.True);
            Assert.That(multiplayClient.JoinedClients[eventHandler.ClientId], Is.EqualTo(multiplayClient.LocalClient));
        });

        [Test]
        public void JoinWithJoiningConfigNull()
            => Assert.That(() => multiplayClient.JoinAsync(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("joiningConfig"));

        [UnityTest]
        public IEnumerator JoiningApprovalRejected() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("JoiningApprovalReject");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);

            Assert.That(eventHandler.IsJoiningApprovalRejected, Is.False);
            Assert.That(eventHandler.ClientId, Is.Null);
            Assert.That(multiplayClient.LocalClient, Is.Null);
            Assert.That(multiplayClient.JoinedClients.Count, Is.Zero);

            await multiplayClient.JoinAsync(joiningConfig);

            Assert.That(eventHandler.IsJoiningApprovalRejected, Is.True);
            Assert.That(eventHandler.ClientId, Is.Null);
            Assert.That(multiplayClient.LocalClient, Is.Null);
            Assert.That(multiplayClient.JoinedClients.Count, Is.Zero);
        });

        [UnityTest]
        public IEnumerator LeaveSuccess() => UniTask.ToCoroutine(async () =>
        {
            Assert.That(eventHandler.LeavingReason, Is.Null);
            await multiplayClient.LeaveAsync();
            Assert.That(eventHandler.LeavingReason, Is.EqualTo("leave request"));
        });

        [Test]
        public void UnexpectedLeft()
        {
            Assert.That(eventHandler.UnexpectedLeftReason, Is.Null);
            messagingClient.FireOnUnexpectedLeft();
            Assert.That(eventHandler.UnexpectedLeftReason, Is.EqualTo("unknown"));
        }

        [UnityTest]
        public IEnumerator ClientJoined() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            Assert.That(eventHandler.JoinedClientId, Is.Null);

            messagingClient.FireOnClientJoined();
            await AssertLogAppearsInSomeFramesAsync($"\"command\":{(int)MultiplayMessageCommand.CreateExistedObject}", LogType.Log);

            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.JoinedClientId));
        });

        [UnityTest]
        public IEnumerator CreateExistedObjectFromOthers() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            Assert.That(eventHandler.JoinedClientId, Is.Null);

            messagingClient.FireCreateExistedObjectFromOthers(networkObjectsProvider.NetworkObject);

            await AssertLogAppearsInSomeFramesAsync($"\"command\":{(int)MultiplayMessageCommand.ClientInitialized}", LogType.Log);
            await AssertObjectIsExpectValueInSomeFramesAsync(multiplayClient.JoinedClients, nameof(multiplayClient.JoinedClients.Count), 2);
            var joinedClient = multiplayClient.JoinedClients.First(pair => pair.Value.NetworkObjects.Count > 0).Value;
            Assert.That(joinedClient.NetworkObjects[0], Is.EqualTo(eventHandler.SpawnedObject));
        });

        [Test]
        public void ClientLeaving()
        {
            Assert.That(eventHandler.LeavingClientId, Is.Null);
            messagingClient.FireOnClientLeaving();
            Assert.That(eventHandler.LeavingClientId, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ClientLeavingAfterSpawnObject() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            messagingClient.FireOnClientJoined();
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.JoinedClientId));

            messagingClient.SpawnObjectFromOthers(networkObjectsProvider.NetworkObject);
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.SpawnedObject));

            messagingClient.FireOnClientLeaving();
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.SpawnedObject), isNull: true);
        });

        [UnityTest]
        public IEnumerator SpawnObjectWithMessageSuccess() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            const string message = "TestMessage";

            Assert.That(eventHandler.SpawnedObject, Is.Null);
            Assert.That(multiplayClient.LocalClient.NetworkObjects.Count, Is.Zero);

            var spawnedObject = multiplayClient.SpawnObject(networkObjectsProvider.NetworkObject, message: message);

            Assert.That(spawnedObject.name, Does.Contain("Object"));
            Assert.That(eventHandler.SpawnedObjectClientId, Is.EqualTo(eventHandler.ClientId));
            Assert.That(eventHandler.SpawnedObject, Is.EqualTo(spawnedObject));
            Assert.That(eventHandler.SpawnedObjectMessage, Is.EqualTo(message));
            Assert.That(multiplayClient.LocalClient.NetworkObjects.Count, Is.EqualTo(1));
            Assert.That(multiplayClient.LocalClient.NetworkObjects[0], Is.EqualTo(spawnedObject));
            await AssertLogAppearsInSomeFramesAsync($"\"command\":{(int)MultiplayMessageCommand.Create}", LogType.Log);
        });

        [UnityTest]
        public IEnumerator SpawnObjectWithoutMessageSuccess() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            Assert.That(eventHandler.SpawnedObject, Is.Null);
            Assert.That(multiplayClient.LocalClient.NetworkObjects.Count, Is.Zero);

            var spawnedObject = multiplayClient.SpawnObject(networkObjectsProvider.NetworkObject);

            Assert.That(spawnedObject.name, Does.Contain("Object"));
            Assert.That(eventHandler.SpawnedObjectClientId, Is.EqualTo(eventHandler.ClientId));
            Assert.That(eventHandler.SpawnedObject, Is.EqualTo(spawnedObject));
            Assert.That(eventHandler.SpawnedObjectMessage, Is.Null);
            Assert.That(multiplayClient.LocalClient.NetworkObjects.Count, Is.EqualTo(1));
            Assert.That(multiplayClient.LocalClient.NetworkObjects[0], Is.EqualTo(spawnedObject));
            await AssertLogAppearsInSomeFramesAsync($"\"command\":{(int)MultiplayMessageCommand.Create}", LogType.Log);
        });

        [Test]
        public void SpawnNullObject()
            => Assert.That(() => multiplayClient.SpawnObject(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("objectPrefab"));

        [Test]
        public void SpawnNotRegisteredObject()
            => Assert.That(() => multiplayClient.SpawnObject(networkObjectsProvider.SpawnFailedObject),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contains("Specify any of the objects that INetworkObjectsProvider provides"));

        [UnityTest]
        public IEnumerator SynchronizeToOthers() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            var spawnedObject = multiplayClient.SpawnObject(networkObjectsProvider.NetworkObject);
            await AssertLogAppearsInSomeFramesAsync($"\"command\":{(int)MultiplayMessageCommand.Update}", LogType.Log);
        });

        [UnityTest]
        public IEnumerator ReceiveSpawnObjectFromOthers() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            messagingClient.FireOnClientJoined();
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.JoinedClientId));

            messagingClient.SpawnObjectFromOthers(networkObjectsProvider.NetworkObject);
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.SpawnedObject));
            Assert.That(eventHandler.SpawnedObjectClientId, Is.EqualTo(eventHandler.JoinedClientId));
        });

        [UnityTest]
        public IEnumerator ReceiveSynchronizeObjectFromOthers() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            messagingClient.FireOnClientJoined();
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.JoinedClientId));

            messagingClient.SpawnObjectFromOthers(networkObjectsProvider.NetworkObject);
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.SpawnedObject));

            await AssertObjectIsExpectValueInSomeFramesAsync(eventHandler.SpawnedObject.transform, nameof(eventHandler.SpawnedObject.transform.position), Vector3.zero);
            messagingClient.UpdateObjectFromOthers();
            await AssertObjectIsExpectValueInSomeFramesAsync(eventHandler.SpawnedObject.transform, nameof(eventHandler.SpawnedObject.transform.position), Vector3.forward);
        });

        [UnityTest]
        public IEnumerator SendMessageSuccess() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            const string message = "TestMessage";
            multiplayClient.SendMessage(message);
            await AssertLogAppearsInSomeFramesAsync(message, LogType.Log);
        });

        [Test]
        public void SendNullMessage()
            => Assert.That(() => multiplayClient.SendMessage(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("message"));

        [UnityTest]
        public IEnumerator ReceivedMessage() => UniTask.ToCoroutine(async () =>
        {
            var messagingJoiningConfig = new MessagingJoiningConfig("MultiplayTest");
            var joiningConfig = new MultiplayJoiningConfig(messagingJoiningConfig);
            await multiplayClient.JoinAsync(joiningConfig);

            const string message = "TestMessage";

            Assert.That(eventHandler.ReceivedMessage, Is.Null);
            messagingClient.FireOnMessageReceived(message);
            await AssertObjectIsNullOrNotInSomeFramesAsync(eventHandler, nameof(eventHandler.ReceivedMessage));
            Assert.That(eventHandler.ReceivedMessage, Is.EqualTo(message));
        });

        private static async UniTask AssertObjectIsNullOrNotInSomeFramesAsync(object obj, string propertyName, bool isNull = false, int frames = 10)
        {
            var type = obj.GetType();
            var property = type.GetProperty(propertyName);

            for (var i = 0; i < frames && property.GetValue(obj) == null != isNull; i++)
            {
                await UniTask.Yield();
            }
            Assert.That(property.GetValue(obj), Is.Not.Null);
        }

        private static async UniTask AssertObjectIsExpectValueInSomeFramesAsync(object obj, string propertyName, object expectValue, int frames = 10)
        {
            var type = obj.GetType();
            var property = type.GetProperty(propertyName);

            for (var i = 0; i < frames && property.GetValue(obj) != expectValue; i++)
            {
                await UniTask.Yield();
            }
            Assert.That(property.GetValue(obj), Is.EqualTo(expectValue));
        }

        private static async UniTask AssertLogAppearsInSomeFramesAsync(string logFragment, LogType logType, int frames = 10)
        {
            var logMessages = new Queue<string>();
            Application.LogCallback logMessageReceivedHandler = (string condition, string stackTrace, LogType type) =>
            {
                if (type == logType)
                {
                    logMessages.Enqueue(condition);
                }
            };
            Application.logMessageReceived += logMessageReceivedHandler;

            for (var i = 0; i < frames; i++)
            {
                while (logMessages.Count > 0)
                {
                    var logMessage = logMessages.Dequeue();
                    if (logMessage.Contains(logFragment))
                    {
                        Application.logMessageReceived -= logMessageReceivedHandler;
                        return;
                    }
                }
                await UniTask.Yield();
            }
            Assert.Fail();
        }

        private class EventHandler
        {
            public string ClientId { get; private set; }
            public void SetClientId(string clientId)
                => ClientId = clientId;

            public string LeavingReason { get; private set; }
            public void SetLeavingReason(string reason)
                => LeavingReason = reason;

            public string UnexpectedLeftReason { get; private set; }
            public void SetUnexpectedLeftReason(string reason)
                => UnexpectedLeftReason = reason;

            public bool IsJoiningApprovalRejected { get; private set; }
            public void SetIsJoiningApprovalRejected(bool isJoiningApprovalRejected)
                => IsJoiningApprovalRejected = isJoiningApprovalRejected;

            public string JoinedClientId { get; private set; }
            public void SetJoinedClientId(string clientId)
                => JoinedClientId = clientId;

            public string LeavingClientId { get; private set; }
            public void SetLeavingClientId(string clientId)
                => LeavingClientId = clientId;

            public string ReceivedMessageFrom { get; private set; }
            public string ReceivedMessage { get; private set; }
            public void SetReceivedMessageInfo((string from, string message) values)
            {
                ReceivedMessageFrom = values.from;
                ReceivedMessage = values.message;
            }

            public string SpawnedObjectClientId { get; private set; }
            public GameObject SpawnedObject { get; private set; }
            public string SpawnedObjectMessage { get; private set; }
            public void SetSpawnedObjectInfo((string clientId, GameObject spawnedObject, string message) values)
            {
                SpawnedObjectClientId = values.clientId;
                SpawnedObject = values.spawnedObject;
                SpawnedObjectMessage = values.message;
            }

            public void Clear()
            {
                SetClientId(default);
                SetLeavingReason(default);
                SetUnexpectedLeftReason(default);
                SetIsJoiningApprovalRejected(default);
                SetJoinedClientId(default);
                SetLeavingClientId(default);
                SetReceivedMessageInfo(default);
                SetSpawnedObjectInfo(default);
            }
        }
    }

}
