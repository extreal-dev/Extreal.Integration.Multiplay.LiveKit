using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UniRx;

namespace Extreal.Integration.Multiplay.Common
{
    public interface IExtrealMultiplayTransport : IDisposable
    {
        bool IsConnected { get; }
        IObservable<string> OnConnected { get; }
        IObservable<string> OnDisconnecting { get; }
        IObservable<string> OnUnexpectedDisconnected { get; }
        IObservable<Unit> OnConnectionApprovalRejected { get; }
        IObservable<string> OnUserConnected { get; }
        IObservable<string> OnUserDisconnecting { get; }

        void EnqueueRequest(string message, string to = default);
        int ResponseQueueCount();
        (string from, string message) DequeueResponse();
        UniTask UpdateAsync();
        UniTask<List<Room>> ListRoomsAsync();
        UniTask ConnectAsync(MultiplayConnectionConfig connectionConfig);
        UniTask DisconnectAsync();
        UniTask DeleteRoomAsync();
    }
}
