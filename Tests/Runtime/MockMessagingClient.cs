using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.Messaging;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging.Test
{
    public class MockMessagingClient : MessagingClient
    {
        private readonly string localClientId = Guid.NewGuid().ToString();
        private readonly string otherClientId = Guid.NewGuid().ToString();
        private GameObject objectPrefab;
        private NetworkObject networkObject;
        private NetworkObjectsProvider networkObjectsProvider;
        private Dictionary<string, GameObject> networkGameObjectDic;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(MockMessagingClient));

        public MockMessagingClient() : base()
        {
        }

#pragma warning disable CS1998
        protected override async UniTask DoJoinAsync(MessagingJoiningConfig connectionConfig)
#pragma warning restore CS1998
        {
            if (connectionConfig.GroupName == "JoiningApprovalReject")
            {
                FireOnJoiningApprovalRejected();
                return;
            }

            FireOnJoined(localClientId);
        }

#pragma warning disable CS1998
        protected override UniTask DoLeaveAsync()
#pragma warning restore CS1998
            => UniTask.CompletedTask;

#pragma warning disable CS1998
        protected override async UniTask DoSendMessageAsync(string message, string to)
#pragma warning restore CS1998
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{nameof(SendMessageAsync)}: message={message}");
            }

            if (message.Contains($"\"Command\":{(int)MultiplayMessageCommand.CreateExistedObject}"))
            {
                var returnMessage = new MultiplayMessage(MultiplayMessageCommand.ClientInitialized).ToJson();
                FireOnMessageReceived(otherClientId, returnMessage);
            }
        }

        public void FireOnUnexpectedLeft()
            => FireOnUnexpectedLeft("unknown");

        public void FireOnClientJoined()
            => FireOnClientJoined(otherClientId);

        public void FireOnClientLeaving()
            => FireOnClientLeaving(otherClientId);

        public void FireOnMessageReceived(string message)
        {
            var messageJson = new MultiplayMessage(MultiplayMessageCommand.Message, message: message).ToJson();
            FireOnMessageReceived(otherClientId, messageJson);
        }

        public void SpawnObjectFromOthers(GameObject objectPrefab)
        {
            this.objectPrefab = objectPrefab;
            var gameObjectKey = GetNetworkGameObjectKey(this.objectPrefab);
            networkObject = new NetworkObject(gameObjectKey, default, Quaternion.identity);
            var messageJson = new MultiplayMessage(MultiplayMessageCommand.Create, networkObject: networkObject).ToJson();
            FireOnMessageReceived(otherClientId, messageJson);
        }

        public void UpdateObjectFromOthers()
        {
            var go = new GameObject();
            go.transform.position = Vector3.forward;
            networkObject.GetTransformFrom(go.transform);
            UnityEngine.Object.Destroy(go);

            if (objectPrefab.TryGetComponent(out PlayerInput input))
            {
                networkObject.GetValuesFrom(in input);
            }

            var message = new MultiplayMessage(MultiplayMessageCommand.Update, networkObjects: new NetworkObject[] { networkObject }).ToJson();
            FireOnMessageReceived(otherClientId, message);
        }

        public void FireCreateExistedObjectFromOthers(GameObject objectPrefab)
        {
            var gameObjectKey = GetNetworkGameObjectKey(objectPrefab);
            var networkObject = new NetworkObject(gameObjectKey, default, Quaternion.identity);
            var networkObjects = new NetworkObject[] { networkObject };
            var message = new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjects: networkObjects).ToJson();
            FireOnMessageReceived(otherClientId, message);
        }

        protected override UniTask<GroupListResponse> DoListGroupsAsync()
        {
            var group = new GroupResponse
            {
                Name = "TestName"
            };

            var groups = new GroupListResponse { Groups = new List<GroupResponse> { group } };
            return UniTask.FromResult(groups);
        }

        private string GetNetworkGameObjectKey(GameObject objectPrefab)
        {
            networkObjectsProvider = UnityEngine.Object.FindObjectOfType<NetworkObjectsProvider>();
            networkGameObjectDic = networkObjectsProvider.Provide();
            var gameObjectKey = networkGameObjectDic.FirstOrDefault(keyValue => keyValue.Value == objectPrefab).Key;
            if (string.IsNullOrEmpty(gameObjectKey) || !networkGameObjectDic.ContainsKey(gameObjectKey))
            {
                gameObjectKey = "failed";
            }
            return gameObjectKey;
        }
    }
}
