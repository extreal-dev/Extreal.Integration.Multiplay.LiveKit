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
        private NetworkObject networkObjectInfo;
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

            SetJoiningGroupStatus(true);
            FireOnJoined(localClientId);
        }

#pragma warning disable CS1998
        protected override async UniTask DoLeaveAsync()
#pragma warning restore CS1998
            => SetJoiningGroupStatus(false);

#pragma warning disable CS1998
        protected override async UniTask DoSendMessageAsync(string message, string to)
#pragma warning restore CS1998
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"{nameof(SendMessageAsync)}: message={message}");
            }

            if (message.Contains($"\"command\":{(int)MultiplayMessageCommand.CreateExistedObject}"))
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
            networkObjectInfo = new NetworkObject(gameObjectKey, default, default);
            var messageJson = new MultiplayMessage(MultiplayMessageCommand.Create, networkObjectInfo: networkObjectInfo).ToJson();
            FireOnMessageReceived(otherClientId, messageJson);
        }

        public void UpdateObjectFromOthers()
        {
            var go = new GameObject();
            go.transform.position = Vector3.forward;
            networkObjectInfo.GetTransformFrom(go.transform);
            UnityEngine.Object.Destroy(go);

            if (objectPrefab.TryGetComponent(out PlayerInput input))
            {
                networkObjectInfo.GetValuesFrom(in input);
            }

            var message = new MultiplayMessage(MultiplayMessageCommand.Update, networkObjectInfos: new NetworkObject[] { networkObjectInfo }).ToJson();
            FireOnMessageReceived(otherClientId, message);
        }

        public void FireCreateExistedObjectFromOthers(GameObject objectPrefab)
        {
            var gameObjectKey = GetNetworkGameObjectKey(objectPrefab);
            var networkObjectInfo = new NetworkObject(gameObjectKey, default, default);
            var networkObjectInfos = new NetworkObject[] { networkObjectInfo };
            var message = new MultiplayMessage(MultiplayMessageCommand.CreateExistedObject, networkObjectInfos: networkObjectInfos).ToJson();
            FireOnMessageReceived(otherClientId, message);
        }

        protected override UniTask<GroupListResponse> DoListGroupsAsync()
        {
            var group = new GroupResponse
            {
                Id = "TestId",
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
