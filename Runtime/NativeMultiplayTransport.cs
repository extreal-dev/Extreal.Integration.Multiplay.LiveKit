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
        public bool IsConnected { get; private set; }
        public List<string> ConnectedParticipants => new List<string>();

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
        private string relayServerUrl = "http://localhost:3000";
        private string roomName;
        private string userIdentity;

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

            ioClient = new SocketIO(relayServerUrl, new SocketIOOptions { EIO = SocketIOClient.EngineIO.V4 }).AddTo(disposables);

            ioClient.OnConnected += ConnectedEventHandler;
            ioClient.OnDisconnected += DisconnectedEventHandler;

            ioClient.On("user connected", UserConnectedEventHandler);
            ioClient.On("user disconnecting", UserDisconnectingEventHandler);
            ioClient.On("message", MessageReceivedEventHandler);
        }

        private void UserDisconnectingEventHandler(SocketIOResponse response) => throw new NotImplementedException();

        private void ConnectedEventHandler(object sender, EventArgs e)
        {
            IsConnected = true;
            userIdentity = Guid.NewGuid().ToString();
            var message = new MultiplayMessage(userIdentity, roomName, MultiplayMessageCommand.Join);
            SendMessageAsync(message.ToJson()).Forget();
            onConnected.OnNext(userIdentity);
        }

        protected override void ReleaseManagedResources()
        {
            ioClient.OnConnected -= ConnectedEventHandler;
            ioClient.OnDisconnected -= DisconnectedEventHandler;

            ioClient.Dispose();
            ioClient = null;

            disposables.Dispose();
        }

        public void Update()
        {
            while (requestQueue.Count > 0)
            {
                var message = requestQueue.Dequeue();
                var jsonMsg = message.ToJson();
                if (ioClient != null && ioClient.Connected)
                {
                    SendMessageAsync(jsonMsg).Forget();
                }
            }
        }

        private async UniTask SendMessageAsync(string jsonMsg)
        {
            await ioClient.EmitAsync("message", jsonMsg);
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
            using var uwr = UnityWebRequest.Get($"{relayServerUrl}/listRooms");
            await uwr.SendWebRequest();

            var roomList = JsonUtility.FromJson<RoomList>(uwr.downloadHandler.text);
            return roomList.Rooms;
        }

        public async UniTask ConnectAsync(ConnectionConfig connectionConfig)
        {
            if (connectionConfig is not LiveKitConnectionConfig liveKitConnectionConfig)
            {
                throw new ArgumentException("Expect LiveKitConnectionConfig", nameof(connectionConfig));
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: url={liveKitConnectionConfig.Url}, token={liveKitConnectionConfig.AccessToken}");
            }
            roomName = liveKitConnectionConfig.RoomName;
            await ioClient.ConnectAsync();
        }

        public async Task DisconnectAsync()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(DisconnectAsync));
            }

            IsConnected = false;
            onDisconnecting.OnNext(Unit.Default);

            roomName = string.Empty;
            requestQueue.Clear();
            responseQueue.Clear();
            await ioClient.DisconnectAsync();
            ioClient.Dispose();
            ioClient = null;

        }

        public async UniTask DeleteRoomAsync()
        {
            using var uwr = UnityWebRequest.Get($"{relayServerUrl}/deleteRoom?RoomName={roomName}");
            await uwr.SendWebRequest();

            roomName = string.Empty;
        }

        private void DisconnectedEventHandler(object sender, string e)
        {
            onUnexpectedDisconnected.OnNext(e);
        }

        private void UserConnectedEventHandler(SocketIOResponse response)
        {
            var jsonStr = response.GetValue<string>();
            var message = JsonUtility.FromJson<MultiplayMessage>(jsonStr);
            onUserConnected.OnNext(message.NetworkObjectInfo.ObjectGuid.ToString());
        }

        private void MessageReceivedEventHandler(SocketIOResponse response)
        {
            var dataStr = response.GetValue<string>();
            var message = JsonUtility.FromJson<MultiplayMessage>(dataStr);

            responseQueue.Enqueue(("message", message));
        }

        [Serializable]
        private class RoomList
        {
            public RoomInfo[] Rooms => rooms;
            [SerializeField] private RoomInfo[] rooms;

            public string Message => message;
            [SerializeField] private string message;
        }
    }
}

#endif
