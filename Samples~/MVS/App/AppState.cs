using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.Screens.ConfirmationScreen;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.App
{
    [SuppressMessage("Usage", "CC0033")]
    public class AppState : DisposableBase
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(AppState));

        private PeerRole role = PeerRole.Host;
        private CommunicationMode communicationMode = CommunicationMode.Massively;

        public string PlayerName { get; private set; } = "Guest";
        public bool IsHost => role == PeerRole.Host;
        public bool IsClient => role == PeerRole.Client;
        public bool IsMassivelyForCommunication => communicationMode == CommunicationMode.Massively;
        public string GroupName { get; private set; } // Host only
        public string GroupId { get; private set; } // Client only
        public string SpaceName { get; private set; }

        public IReadOnlyReactiveProperty<bool> PlayingReady => playingReady.AddTo(disposables);
        private readonly ReactiveProperty<bool> playingReady = new ReactiveProperty<bool>(false);

        public IReadOnlyReactiveProperty<bool> SpaceReady => spaceReady.AddTo(disposables);
        private readonly ReactiveProperty<bool> spaceReady = new ReactiveProperty<bool>(false);

        public IReadOnlyReactiveProperty<bool> MultiplayReady => multiplayReady;
        private readonly BoolReactiveProperty multiplayReady = new BoolReactiveProperty(false);

        public IObservable<string> OnNotificationReceived => onNotificationReceived.AddTo(disposables);
        private readonly Subject<string> onNotificationReceived = new Subject<string>();

        public IObservable<Confirmation> OnConfirmationReceived => onConfirmationReceived.AddTo(disposables);
        private readonly Subject<Confirmation> onConfirmationReceived = new Subject<Confirmation>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public StageState StageState { get; private set; }

        public AppState()
        {
            multiplayReady.AddTo(disposables);

            MonitorPlayingReadyStatus();
        }

        [SuppressMessage("Usage", "CC0033")]
        private void MonitorPlayingReadyStatus() =>
            multiplayReady.Merge(spaceReady)
                .Where(_ =>
                {
                    LogWaitingStatus();
                    return multiplayReady.Value && spaceReady.Value;
                })
                .Subscribe(_ =>
                {
                    if (Logger.IsDebug())
                    {
                        Logger.LogDebug("Ready to play");
                    }
                    playingReady.Value = true;
                })
                .AddTo(disposables);

        private void LogWaitingStatus()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Multiplay, Space Ready: " +
                                $"{multiplayReady.Value}, {spaceReady.Value}");
            }
        }

        public void SetPlayerName(string playerName) => PlayerName = playerName;
        public void SetRole(PeerRole role) => this.role = role;
        public void SetCommunicationMode(CommunicationMode communicationMode) => this.communicationMode = communicationMode;
        public void SetGroupName(string groupName) => GroupName = groupName;
        public void SetGroupId(string groupId) => GroupId = groupId;
        public void SetSpaceName(string spaceName) => SpaceName = spaceName;
        public void SetMultiplayReady(bool ready) => multiplayReady.Value = ready;
        public void SetSpaceReady(bool ready) => spaceReady.Value = ready;
        public void SetStage(StageName stageName) => StageState = new StageState(stageName);

        public void Notify(string message)
        {
            Logger.LogError(message);
            onNotificationReceived.OnNext(message);
        }

        public void Confirm(Confirmation confirmation)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Confirmation received: {confirmation.Message}");
            }
            onConfirmationReceived.OnNext(confirmation);
        }

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
