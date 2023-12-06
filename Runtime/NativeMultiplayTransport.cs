using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UniRx;
using System.Linq;
using Extreal.Integration.Messaging.Common;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public class NativeMultiplayTransport : DisposableBase, IExtrealMultiplayTransport
    {
        public bool IsConnected => messagingClient != null && messagingClient.IsConnected;

        public IObservable<string> OnConnected => messagingClient.OnConnected;
        public IObservable<string> OnDisconnecting => messagingClient.OnDisconnecting;
        public IObservable<string> OnUnexpectedDisconnected => messagingClient.OnUnexpectedDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected => messagingClient.OnConnectionApprovalRejected;
        public IObservable<string> OnUserConnected => messagingClient.OnUserConnected;
        public IObservable<string> OnUserDisconnecting => messagingClient.OnUserDisconnecting;
        public IObservable<(string, string)> OnMessageReceived => onMessageReceived;
        private readonly Subject<(string, string)> onMessageReceived;

        private readonly ExtrealMessagingClient messagingClient;

        private readonly Queue<(string, MultiplayMessage)> requestQueue = new Queue<(string, MultiplayMessage)>();
        private readonly Queue<(string, MultiplayMessage)> responseQueue = new Queue<(string, MultiplayMessage)>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public void EnqueueRequest(MultiplayMessage message, string to = default)
            => requestQueue.Enqueue((to, message));

        public int ResponseQueueCount()
            => responseQueue.Count;

        public (string userIdentity, MultiplayMessage message) DequeueResponse()
            => responseQueue.Count != 0 ? responseQueue.Dequeue() : (null, null);

        [SuppressMessage("Usage", "CC0022")]
        public NativeMultiplayTransport(IExtrealMessagingTransport messagingTransport)
        {
            onMessageReceived = new Subject<(string, string)>().AddTo(disposables);
            messagingClient = new ExtrealMessagingClient().AddTo(disposables);

            messagingClient.SetTransport(messagingTransport);

            messagingClient.OnMessageReceived
                .Subscribe(values =>
                {
                    var multiplayMessage = JsonUtility.FromJson<MultiplayMessage>(values.message);

                    if (multiplayMessage.MultiplayMessageCommand == MultiplayMessageCommand.Message)
                    {
                        onMessageReceived.OnNext((values.userId, multiplayMessage.Message));
                        return;
                    }

                    responseQueue.Enqueue((values.userId, multiplayMessage));
                })
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public async UniTask UpdateAsync()
        {
            while (requestQueue.Count > 0)
            {
                (var to, var message) = requestQueue.Dequeue();
                var jsonMsg = message.ToJson();
                if (IsConnected)
                {
                    await messagingClient.SendMessageAsync(jsonMsg, to);
                }
            }
        }

        public async UniTask<List<MultiplayRoomInfo>> ListRoomsAsync()
        {
            var roomInfos = await messagingClient.ListRoomsAsync();
            return roomInfos.Select(roomInfo => new MultiplayRoomInfo(roomInfo.Id, roomInfo.Name)).ToList();
        }

        public async UniTask ConnectAsync(MultiplayConnectionConfig connectionConfig)
        {
            if (connectionConfig is not MessagingMultiplayConnectionConfig messagingMultiplayConnectionConfig)
            {
                throw new ArgumentException("Expected MessagingMultiplayConnectionConfig", nameof(connectionConfig));
            }

            await messagingClient.ConnectAsync(messagingMultiplayConnectionConfig.MessagingConnectionConfig);
        }

        public UniTask DisconnectAsync()
            => messagingClient.DisconnectAsync();

        public UniTask DeleteRoomAsync()
            => messagingClient.DeleteRoomAsync();
    }
}
