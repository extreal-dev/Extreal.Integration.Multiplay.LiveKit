#if !UNITY_WEBGL || UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UniRx;
using Extreal.Integration.Messaging.Common;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public class MessagingMultiplayTransport : DisposableBase, IExtrealMultiplayTransport
    {
        public bool IsConnected => messagingClient != null && messagingClient.IsConnected;

        public IObservable<string> OnConnected => messagingClient.OnConnected;
        public IObservable<Unit> OnDisconnecting => messagingClient.OnDisconnecting;
        public IObservable<string> OnUnexpectedDisconnected => messagingClient.OnUnexpectedDisconnected;
        public IObservable<Unit> OnConnectionApprovalRejected => messagingClient.OnConnectionApprovalRejected;
        public IObservable<string> OnUserConnected => messagingClient.OnUserConnected;
        public IObservable<string> OnUserDisconnecting => messagingClient.OnUserDisconnecting;
        public IObservable<(string, string)> OnMessageReceived => onMessageReceived;
        private readonly Subject<(string, string)> onMessageReceived;

        private readonly MessagingClient messagingClient;

        private readonly Queue<MultiplayMessage> requestQueue = new Queue<MultiplayMessage>();
        private readonly Queue<(string, MultiplayMessage)> responseQueue = new Queue<(string, MultiplayMessage)>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(MessagingMultiplayTransport));

        public void EnqueueRequest(MultiplayMessage message)
            => requestQueue.Enqueue(message);

        public int ResponseQueueCount()
            => responseQueue.Count;

        public (string userIdentity, MultiplayMessage message) DequeueResponse()
            => responseQueue.Count != 0 ? responseQueue.Dequeue() : (null, null);

        [SuppressMessage("Usage", "CC0022")]
        public MessagingMultiplayTransport()
        {
            onMessageReceived = new Subject<(string, string)>().AddTo(disposables);
            messagingClient = new MessagingClient().AddTo(disposables);

            messagingClient.OnMessageReceived
                .Subscribe(values =>
                {
                    var multiplayMessage = JsonUtility.FromJson<MultiplayMessage>(values.message);
                    if (multiplayMessage.MultiplayMessageCommand is MultiplayMessageCommand.Message)
                    {
                        onMessageReceived.OnNext((values.userId, multiplayMessage.Message));
                    }
                    else
                    {
                        responseQueue.Enqueue((values.userId, multiplayMessage));
                    }
                });
        }

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public void SetMessagingTransport(IExtrealMessagingTransport messagingTransport)
            => messagingClient.SetTransport(messagingTransport);

        public void Update()
        {
            while (requestQueue.Count > 0)
            {
                var message = requestQueue.Dequeue();
                var jsonMsg = message.ToJson();
                if (IsConnected)
                {
                    messagingClient.SendMessageAsync(jsonMsg).Forget();
                }
            }
        }

        public async UniTask<MultiplayRoomInfo[]> ListRoomsAsync()
        {
            var messagingRoomInfos = await messagingClient.ListRoomsAsync();
            return messagingRoomInfos.Select(messagingRoomInfo => new MultiplayRoomInfo(messagingRoomInfo.Id, messagingRoomInfo.Name)).ToArray();
        }

        public async UniTask ConnectAsync(MultiplayConnectionConfig connectionConfig)
        {
            if (connectionConfig is not MessagingMultiplayConnectionConfig messagingMultiplayConnectionConfig)
            {
                throw new ArgumentException("Except MessagingMultiplayConnectionConfig", nameof(connectionConfig));
            }
            await messagingClient.ConnectAsync(messagingMultiplayConnectionConfig.MessagingConnectionConfig);
        }

        public void Disconnect()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Disconnect));
            }

            requestQueue.Clear();
            responseQueue.Clear();
            messagingClient.Disconnect();
        }

        public UniTask DeleteRoomAsync()
            => messagingClient.DeleteRoomAsync();
    }
}

#endif
