using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.App
{
    [SuppressMessage("Usage", "CC0033")]
    public class AppState : DisposableBase
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(AppState));

        private UserRole role = UserRole.Host;
        private CommunicationMode communicationMode = CommunicationMode.Massively;

        public string PlayerName { get; private set; } = "Guest";
        public bool IsHost => role == UserRole.Host;
        public bool IsClient => role == UserRole.Client;
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
        public void SetRole(UserRole role) => this.role = role;
        public void SetCommunicationMode(CommunicationMode communicationMode) => this.communicationMode = communicationMode;
        public void SetGroupName(string groupName) => GroupName = groupName;
        public void SetGroupId(string groupId) => GroupId = groupId;
        public void SetSpaceName(string spaceName) => SpaceName = spaceName;
        public void SetMultiplayReady(bool ready) => multiplayReady.Value = ready;
        public void SetSpaceReady(bool ready) => spaceReady.Value = ready;

        public void Notify(string message)
        {
            Logger.LogError(message);
            onNotificationReceived.OnNext(message);
        }

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }

    public enum UserRole
    {
        None = 0,
        Host = 1,
        Client = 2,
    }
}
