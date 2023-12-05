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

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class NativeMultiplayTransport : DisposableBase
    {
        public bool IsConnected => ioClient != null && ioClient.Connected;
        public List<string> ConnectedUserIdentities => new List<string>();

        public IObservable<string> OnConnected => onConnected;
        private readonly Subject<string> onConnected;

        public IObservable<Unit> OnDisconnecting => onDisconnecting;
        private readonly Subject<Unit> onDisconnecting;

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
            onDisconnecting = new Subject<Unit>().AddTo(disposables);
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

        public async UniTask<RoomInfo[]> ListRoomsAsync()
        {
            var rooms = new RoomInfo[]
            {
                new RoomInfo("1", "aaa"),
                new RoomInfo("2", "bbb")
            };

            // using var uwr = UnityWebRequest.Get($"{relayServerUrl}/listRooms");
            // await uwr.SendWebRequest();

            // var roomList = JsonUtility.FromJson<RoomList>(uwr.downloadHandler.text);
            // return roomList.Rooms;
            return rooms;
        }
        public async UniTask ConnectAsync(string roomName)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: roomName={roomName}");
            }

            userIdentityLocal = Guid.NewGuid().ToString();
            await (await GetSocketAsync()).EmitAsync("join", userIdentityLocal, roomName);

            onConnected.OnNext(userIdentityLocal);
        }

        public async Task DisconnectAsync()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(DisconnectAsync));
            }
            onDisconnecting.OnNext(Unit.Default);
            StopSocket();
        }

        public async UniTask DeleteRoomAsync()
        {
            using var uwr = UnityWebRequest.Get($"{relayServerUrl}/deleteRoom?RoomName={roomName}");
            await uwr.SendWebRequest();

            roomName = string.Empty;
        }

        private async void DisconnectedEventHandler(object sender, string e)
        {
            await UniTask.SwitchToMainThread();
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

            var multiplayMessage = JsonUtility.FromJson<MultiplayMessage>(message.MessageContent);

            var userIdentityRemote = message.From;

            if (multiplayMessage.MultiplayMessageCommand == MultiplayMessageCommand.Message)
            {
                onMessageReceived.OnNext((userIdentityRemote, multiplayMessage.Message));
                return;
            }

            responseQueue.Enqueue((userIdentityRemote, multiplayMessage));
        }

        [Serializable]
        private class RoomList
        {
            public RoomInfo[] Rooms => rooms;
            [SerializeField] private RoomInfo[] rooms;

            public string Message => message;
            [SerializeField] private string message;
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
