using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Extreal.Integration.Multiplay.Common
{
    public interface IExtrealMultiplayTransport : IDisposable
    {
        public bool IsConnected { get; }
        IObservable<string> OnConnected { get; }
        IObservable<Unit> OnDisconnecting { get; }
        IObservable<string> OnUnexpectedDisconnected { get; }
        IObservable<Unit> OnConnectionApprovalRejected { get; }
        IObservable<string> OnUserConnected { get; }
        IObservable<string> OnUserDisconnecting { get; }
        IObservable<(string, string)> OnMessageReceived { get; }

        public void EnqueueRequest(MultiplayMessage message);
        public int ResponseQueueCount();
        public (string userIdentity, MultiplayMessage message) DequeueResponse();
        public void Update();
        public UniTask<MultiplayRoomInfo[]> ListRoomsAsync();
        public UniTask ConnectAsync(MultiplayConnectionConfig connectionConfig);
        public void Disconnect();
        public UniTask DeleteRoomAsync();
    }
}
