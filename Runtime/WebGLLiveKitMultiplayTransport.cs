#if UNITY_WEBGL // && !UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using LiveKit;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEngine;
using UniRx;
using UnityEngine.Networking;
using System.Linq;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class WebGLLiveKitMultiplayTransport : DisposableBase
    {
        public bool IsConnected { get; private set; }
        public List<RemoteParticipant> ConnectedParticipants => room.Participants.Values.ToList();

        public IObservable<Unit> OnConnected => onConnected;
        private readonly Subject<Unit> onConnected;

        public IObservable<Unit> OnDisconnecting => onDisconnecting;
        private readonly Subject<Unit> onDisconnecting;

        public IObservable<DisconnectReason> OnUnexpectedDisconnected => onUnexpectedDisconnected;
        private readonly Subject<DisconnectReason> onUnexpectedDisconnected;

        public IObservable<Unit> OnConnectionApprovalRejected => onConnectionApprovalRejected;
        private readonly Subject<Unit> onConnectionApprovalRejected;

        public IObservable<RemoteParticipant> OnUserConnected => onUserConnected;
        private readonly Subject<RemoteParticipant> onUserConnected;

        public IObservable<RemoteParticipant> OnUserDisconnected => onUserDisconnected;
        private readonly Subject<RemoteParticipant> onUserDisconnected;

        public IObservable<(Participant, string)> OnMessageReceived => onMessageReceived;
        private readonly Subject<(Participant, string)> onMessageReceived;

        private readonly Room room;
        private string apiServerUrl;
        private string roomName;

        private readonly Queue<LiveKitMultiplayMessage> requestQueue = new Queue<LiveKitMultiplayMessage>();
        private readonly Queue<(Participant, LiveKitMultiplayMessage)> responseQueue = new Queue<(Participant, LiveKitMultiplayMessage)>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLLiveKitMultiplayTransport));

        public void EnqueueRequest(LiveKitMultiplayMessage message)
            => requestQueue.Enqueue(message);

        public int ResponseQueueCount()
            => responseQueue.Count;

        public (Participant participant, LiveKitMultiplayMessage message) DequeueResponse()
            => responseQueue.Count != 0 ? responseQueue.Dequeue() : (null, null);

        [SuppressMessage("Usage", "CC0022")]
        public WebGLLiveKitMultiplayTransport()
        {
            onConnected = new Subject<Unit>().AddTo(disposables);
            onDisconnecting = new Subject<Unit>().AddTo(disposables);
            onUnexpectedDisconnected = new Subject<DisconnectReason>().AddTo(disposables);
            onUserConnected = new Subject<RemoteParticipant>().AddTo(disposables);
            onUserDisconnected = new Subject<RemoteParticipant>().AddTo(disposables);
            onConnectionApprovalRejected = new Subject<Unit>().AddTo(disposables);
            onMessageReceived = new Subject<(Participant, string)>().AddTo(disposables);

            room = new Room().AddTo(disposables);
            room.Disconnected += DisconnectedEventHandler;
            room.ParticipantConnected += ParticipantConnectedEventHandler;
            room.ParticipantDisconnected += ParticipantDisconnectedEventHandler;
            room.DataReceived += DataReceivedEventHandler;
        }

        protected override void ReleaseManagedResources()
        {
            room.Disconnected -= DisconnectedEventHandler;
            room.ParticipantConnected -= ParticipantConnectedEventHandler;
            room.ParticipantDisconnected -= ParticipantDisconnectedEventHandler;
            room.DataReceived -= DataReceivedEventHandler;

            disposables.Dispose();
        }

        public void Update()
        {
            while (requestQueue.Count > 0)
            {
                var message = requestQueue.Dequeue();
                var jsonMsg = message.ToJson();
                if (!room.IsClosed)
                {
                    SendMessageAsync(jsonMsg, message.ToParticipant, message.DataPacketKind).Forget();
                }
            }
        }

        private async UniTask SendMessageAsync(string message, RemoteParticipant participant, DataPacketKind dataPacketKind)
        {
            var data = Encoding.ASCII.GetBytes(message);
            await room.LocalParticipant.PublishData(data, dataPacketKind, participant);
        }

        public void Initialize(TransportConfig transportConfig)
        {
            if (transportConfig is not LiveKitTransportConfig liveKitTransportConfig)
            {
                throw new ArgumentException("Expect LiveKitTransportConfig", nameof(transportConfig));
            }

            apiServerUrl = liveKitTransportConfig.ApiServerUrl;
        }

        public async UniTask<string[]> ListRoomsAsync()
        {
            using var uwr = UnityWebRequest.Get($"{apiServerUrl}/listRooms");
            await uwr.SendWebRequest();

            var roomList = JsonUtility.FromJson<RoomList>(uwr.downloadHandler.text);
            return roomList.Rooms;
        }

        public async UniTask<LocalParticipant> ConnectAsync(ConnectionConfig connectionConfig)
        {
            if (connectionConfig is not LiveKitConnectionConfig liveKitConnectionConfig)
            {
                throw new ArgumentException("Expect LiveKitConnectionConfig", nameof(connectionConfig));
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: url={liveKitConnectionConfig.Url}, token={liveKitConnectionConfig.AccessToken}");
            }

            await room.Connect(liveKitConnectionConfig.Url, liveKitConnectionConfig.AccessToken);
            roomName = liveKitConnectionConfig.RoomName;

            IsConnected = true;

            UniTask.Void(async () =>
            {
                await UniTask.Yield();
                onConnected.OnNext(Unit.Default);
            });

            return room.LocalParticipant;
        }

        public void Disconnect()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Disconnect));
            }

            IsConnected = false;
            onDisconnecting.OnNext(Unit.Default);

            roomName = string.Empty;
            requestQueue.Clear();
            responseQueue.Clear();
            room.Disconnect();
        }

        public async UniTask DeleteRoomAsync()
        {
            using var uwr = UnityWebRequest.Get($"{apiServerUrl}/deleteRoom?RoomName={roomName}");
            await uwr.SendWebRequest();

            roomName = string.Empty;
        }

        private void DisconnectedEventHandler(DisconnectReason? reason)
        {
            var nonNullableReason = reason ?? DisconnectReason.UNKNOWN_REASON;
            onUnexpectedDisconnected.OnNext(nonNullableReason);
        }

        private void ParticipantConnectedEventHandler(RemoteParticipant participant)
            => onUserConnected.OnNext(participant);

        private void ParticipantDisconnectedEventHandler(RemoteParticipant participant)
            => onUserDisconnected.OnNext(participant);

        private void DataReceivedEventHandler(byte[] data, RemoteParticipant participant, DataPacketKind? kind)
        {
            var dataStr = Encoding.ASCII.GetString(data);
            var message = JsonUtility.FromJson<LiveKitMultiplayMessage>(dataStr);
            if (message.LiveKidMultiplayMessageCommand is LiveKidMultiplayMessageCommand.Message)
            {
                onMessageReceived.OnNext((participant, message.Message));
            }
            else
            {
                responseQueue.Enqueue((participant, message));
            }
        }

        [Serializable]
        private class RoomList
        {
            public string[] Rooms => rooms;
            [SerializeField] private string[] rooms;

            public string Message => message;
            [SerializeField] private string message;
        }
    }
}

#endif
