// #if UNITY_WEBGL && !UNITY_EDITOR
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

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class WebGLLiveKitMultiplayTransport : DisposableBase
    {
        public bool IsConnected { get; private set; }

        public IObservable<Unit> OnConnected => onConnected;
        private readonly Subject<Unit> onConnected;

        public IObservable<Unit> OnDisconnected => onDisconnected;
        private readonly Subject<Unit> onDisconnected;

        public IObservable<DisconnectReason> OnUnexpectedDisconnected => onUnexpectedDisconnected;
        private readonly Subject<DisconnectReason> onUnexpectedDisconnected;

        public IObservable<RemoteParticipant> OnUserConnected => onUserConnected;
        private readonly Subject<RemoteParticipant> onUserConnected;

        public IObservable<RemoteParticipant> OnUserDisconnected => onUserDisconnected;
        private readonly Subject<RemoteParticipant> onUserDisconnected;

        public IObservable<Unit> OnConnectionApprovalRejected => onConnectionApprovalRejected;
        private readonly Subject<Unit> onConnectionApprovalRejected;

        public IObservable<(Participant participant, string messageJson)> OnMessageReceived => onMessageReceived;
        private readonly Subject<(Participant, string)> onMessageReceived;

        private readonly Queue<LiveKitMultiplayMessage> requestQueue = new Queue<LiveKitMultiplayMessage>();
        private readonly Queue<LiveKitMultiplayMessage> responseQueue = new Queue<LiveKitMultiplayMessage>();

        private readonly Room room;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLLiveKitMultiplayTransport));

        public int ResponseQueueCount()
            => responseQueue.Count;

        public LiveKitMultiplayMessage DequeueResponse()
            => responseQueue.Count != 0 ? responseQueue.Dequeue() : null;

        [SuppressMessage("Usage", "CC0022")]
        public WebGLLiveKitMultiplayTransport()
        {
            onConnected = new Subject<Unit>().AddTo(disposables);
            onDisconnected = new Subject<Unit>().AddTo(disposables);
            onUnexpectedDisconnected = new Subject<DisconnectReason>().AddTo(disposables);
            onUserConnected = new Subject<RemoteParticipant>().AddTo(disposables);
            onUserDisconnected = new Subject<RemoteParticipant>().AddTo(disposables);
            onConnectionApprovalRejected = new Subject<Unit>().AddTo(disposables);
            onMessageReceived = new Subject<(Participant, string)>().AddTo(disposables);

            room = new Room().AddTo(disposables);
            room.StateChanged += StateChangedEventHandler;
            room.Disconnected += DisconnectedEventHandler;
            room.ParticipantConnected += ParticipantConnectedEventHandler;
            room.ParticipantDisconnected += ParticipantDisconnectedEventHandler;
            room.DataReceived += DataReceivedEventHandler;
        }

        protected override void ReleaseManagedResources()
        {
            room.StateChanged -= StateChangedEventHandler;
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
                var msg = requestQueue.Dequeue();
                var jsonMsg = msg.ToJson();
                if (!room.IsClosed)
                {
                    SendMessageAsync(jsonMsg).Forget();
                }
            }
        }

        public async UniTask ConnectAsync(string url, string token)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Connect: url={url}, token={token}");
            }
            await room.Connect(url, token);
        }

        public void Disconnect()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug(nameof(Disconnect));
            }

            requestQueue.Clear();
            responseQueue.Clear();
            room.Disconnect();
        }

        public async UniTask SendMessageAsync(string message)
        {
            var data = Encoding.ASCII.GetBytes(message);
            await room.LocalParticipant.PublishData(data, DataPacketKind.RELIABLE);
        }

        private void StateChangedEventHandler(ConnectionState state)
        {
            if (state is ConnectionState.Connected)
            {
                IsConnected = true;
                onConnected.OnNext(Unit.Default);
            }
            if (state is ConnectionState.Disconnected)
            {
                IsConnected = false;
                onDisconnected.OnNext(Unit.Default);
            }
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
            if (message.LiveKidMultiplayMessageCommand is LiveKidMultiplayMessageCommand.None)
            {
                onMessageReceived.OnNext((participant, dataStr));
            }
            else
            {
                responseQueue.Enqueue(message);
            }
        }
    }
}

// #endif
