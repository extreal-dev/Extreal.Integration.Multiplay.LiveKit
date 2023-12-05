#if !UNITY_WEBGL && UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEngine;
using UniRx;
using UnityEngine.Networking;
using System.Linq;
using SocketIOClient;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class NativeMultiplayTransport : DisposableBase
    {
        public bool IsConnected { get; private set; }
        public List<string> ConnectedUserIdentities => new List<string>();

        public IObservable<string> OnConnected => onConnected;
        private readonly Subject<string> onConnected;

        public IObservable<string> OnDisconnecting => onDisconnecting;
        private readonly Subject<string> onDisconnecting;

        public IObservable<string> OnUnexpectedDisconnected => onUnexpectedDisconnected;
        private readonly Subject<string> onUnexpectedDisconnected;

        public IObservable<Unit> OnConnectionApprovalRejected => onConnectionApprovalRejected;
        private readonly Subject<Unit> onConnectionApprovalRejected;

        public IObservable<string> OnUserConnected => onUserConnected;
        private readonly Subject<string> onUserConnected;

        public IObservable<string> OnUserDisconnecting => onUserDisconnecting;
        private readonly Subject<string> onUserDisconnecting;

        public IObservable<(string, string)> OnMessageReceived => onMessageReceived;
        private readonly Subject<(string, string)> onMessageReceived;

        private SocketIO ioClient;
        private string relayServerUrl = "http://localhost:3030";
        private string roomName;
        private string userIdentityLocal;



        private readonly Queue<MultiplayMessage> requestQueue = new Queue<MultiplayMessage>();
        private readonly Queue<(string, MultiplayMessage)> responseQueue = new Queue<(string, MultiplayMessage)>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeMultiplayTransport));

        public void EnqueueRequest(MultiplayMessage message)
            => requestQueue.Enqueue(message);

        public int ResponseQueueCount()
            => responseQueue.Count;

        public (string userIdentity, MultiplayMessage message) DequeueResponse()
            => responseQueue.Count != 0 ? responseQueue.Dequeue() : (null, null);

        [SuppressMessage("Usage", "CC0022")]
        public NativeMultiplayTransport()
        {
            onConnected = new Subject<string>().AddTo(disposables);
            onDisconnecting = new Subject<string>().AddTo(disposables);
            onUnexpectedDisconnected = new Subject<string>().AddTo(disposables);
            onUserConnected = new Subject<string>().AddTo(disposables);
            onUserDisconnecting = new Subject<string>().AddTo(disposables);
            onConnectionApprovalRejected = new Subject<Unit>().AddTo(disposables);
            onMessageReceived = new Subject<(string, string)>().AddTo(disposables);
        }

        private async void UserDisconnectingEventHandler(SocketIOResponse response)
        {
            await UniTask.SwitchToMainThread();
            var disconnectingUserIdentity = response.GetValue<string>();
            onUserDisconnecting.OnNext(disconnectingUserIdentity);
        }

        private async UniTask<SocketIO> GetSocketAsync()
        {
            if (ioClient is not null)
            {
                if (ioClient.Connected)
                {
                    return ioClient;
                }
                // Not covered by testing due to defensive implementation
                StopSocket();
            }

            ioClient = new SocketIO(relayServerUrl, new SocketIOOptions { EIO = SocketIOClient.EngineIO.V4 });

            ioClient.OnDisconnected += DisconnectedEventHandler;
            ioClient.On("user connected", UserConnectedEventHandler);
            ioClient.On("user disconnecting", UserDisconnectingEventHandler);
            ioClient.On("message", MessageReceivedEventHandler);

            try
            {
                await ioClient.ConnectAsync().ConfigureAwait(true);
            }
            catch (ConnectionException e)
            {
                throw;
            }

            return ioClient;
        }

        private void StopSocket()
        {
            if (ioClient is null)
            {
                // Not covered by testing due to defensive implementation
                return;
            }
            ioClient.OnDisconnected -= DisconnectedEventHandler;
            ioClient.Dispose();
            ioClient = null;
            IsConnected = false;
        }

        protected override void ReleaseManagedResources()
        {
            StopSocket();
            disposables.Dispose();
        }

        public async UniTask UpdateAsync()
        {
            while (requestQueue.Count > 0)
            {
                var message = requestQueue.Dequeue();
                var jsonMsg = message.ToJson();
                if (ioClient != null && ioClient.Connected)
                {
                    await SendMessageAsync(jsonMsg);
                }
            }
        }

        private async UniTask SendMessageAsync(string jsonMsg)
        {
            var message = JsonUtility.ToJson(new Message(userIdentityLocal, jsonMsg));
            await ioClient.EmitAsync("message", message);
        }

        public void Initialize(TransportConfig transportConfig)
        {
            if (transportConfig is not LiveKitTransportConfig liveKitTransportConfig)
            {
                throw new ArgumentException("Expect LiveKitTransportConfig", nameof(transportConfig));
            }

            relayServerUrl = liveKitTransportConfig.ApiServerUrl;
        }

        public async UniTask<List<RoomInfo>> ListRoomsAsync()
        {
            var roomList = default(RoomList);
            await (await GetSocketAsync()).EmitAsync("list rooms", response =>
            {

                Debug.LogWarning(response.ToString());
                roomList = response.GetValue<RoomList>();
            });
            await UniTask.WaitUntil(() => roomList != null);
            return roomList?.Rooms;
        }

        public async UniTask ConnectAsync(string roomName)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: roomName={roomName}");
            }

            userIdentityLocal = Guid.NewGuid().ToString();
            await (await GetSocketAsync()).EmitAsync("join", userIdentityLocal, roomName);

            IsConnected = true;
            onConnected.OnNext(userIdentityLocal);
        }

        public async Task DisconnectAsync()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(DisconnectAsync));
            }
            onDisconnecting.OnNext("disconnect request");
            StopSocket();
        }

        public UniTask DeleteRoomAsync() => SendMessageAsync("delete room");

        private async void DisconnectedEventHandler(object sender, string e)
        {
            await UniTask.SwitchToMainThread();
            IsConnected = false;
            onUnexpectedDisconnected.OnNext(e);
        }

        private async void UserConnectedEventHandler(SocketIOResponse response)
        {
            await UniTask.SwitchToMainThread();
            var userIdentityRemote = response.GetValue<string>();
            onUserConnected.OnNext(userIdentityRemote);
        }

        private async void MessageReceivedEventHandler(SocketIOResponse response)
        {
            await UniTask.SwitchToMainThread();
            var dataStr = response.GetValue<string>();
            var message = JsonUtility.FromJson<Message>(dataStr);

            if (message.MessageContent == "delete room")
            {
                onDisconnecting.OnNext("delete room");
                StopSocket();
                return;
            }

            var multiplayMessage = JsonUtility.FromJson<MultiplayMessage>(message.MessageContent);

            var userIdentityRemote = message.From;

            if (multiplayMessage.MultiplayMessageCommand == MultiplayMessageCommand.Message)
            {
                onMessageReceived.OnNext((userIdentityRemote, multiplayMessage.Message));
                return;
            }

            responseQueue.Enqueue((userIdentityRemote, multiplayMessage));
        }

        public class RoomList
        {
            [JsonPropertyName("rooms")]
            public List<RoomInfo> Rooms { get; set; }
        }

        [Serializable]
        public class Message
        {
            public string From => from;
            [SerializeField] private string from;

            public string MessageContent => messageContent;
            [SerializeField] private string messageContent;

            public Message(string from, string messageContent)
            {
                this.from = from;
                this.messageContent = messageContent;
            }
        }
    }
}

#endif
