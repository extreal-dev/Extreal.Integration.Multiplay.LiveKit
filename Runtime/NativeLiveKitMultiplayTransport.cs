// Pending
// Remove following pragma when developing
#pragma warning disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using LiveKit;
using UniRx;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class NativeLiveKitMultiplayTransport : DisposableBase
    {
        public bool IsConnected { get; private set; }

        public IObservable<Unit> OnConnected => onConnected;
        private readonly Subject<Unit> onConnected;

        public IObservable<Unit> OnDisconnected => onDisconnected;
        private readonly Subject<Unit> onDisconnected;

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

        private readonly Queue<LiveKitMultiplayMessage> requestQueue = new Queue<LiveKitMultiplayMessage>();
        private readonly Queue<(Participant, LiveKitMultiplayMessage)> responseQueue = new Queue<(Participant, LiveKitMultiplayMessage)>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeLiveKitMultiplayTransport));

        public void EnqueueRequest(LiveKitMultiplayMessage message)
                    => requestQueue.Enqueue(message);

        public int ResponseQueueCount()
            => responseQueue.Count;

        public (Participant participant, LiveKitMultiplayMessage message) DequeueResponse()
            => responseQueue.Count != 0 ? responseQueue.Dequeue() : (null, null);

        [SuppressMessage("Usage", "CC0022")]
        public NativeLiveKitMultiplayTransport()
        {
            onConnected = new Subject<Unit>().AddTo(disposables);
            onDisconnected = new Subject<Unit>().AddTo(disposables);
            onUnexpectedDisconnected = new Subject<DisconnectReason>().AddTo(disposables);
            onUserConnected = new Subject<RemoteParticipant>().AddTo(disposables);
            onUserDisconnected = new Subject<RemoteParticipant>().AddTo(disposables);
            onConnectionApprovalRejected = new Subject<Unit>().AddTo(disposables);
            onMessageReceived = new Subject<(Participant, string)>().AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
            => disposables.Dispose();

        public void Update() => throw new NotImplementedException();
        public async UniTask<Participant> ConnectAsync(string url, string token) => throw new NotImplementedException();
        public void Disconnect() => throw new NotImplementedException();
        public void DeleteRoom() => throw new NotImplementedException();
        public async UniTask SendMessageAsync(string message, DataPacketKind dataPacketKind = DataPacketKind.RELIABLE) => throw new NotImplementedException();
        public async UniTask<Room[]> ListRoomsAsync() => throw new NotImplementedException();
    }
}
