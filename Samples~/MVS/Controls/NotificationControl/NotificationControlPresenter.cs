using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.App.Stages;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.NotificationControl
{
    public class NotificationControlPresenter : StagePresenterBase
    {
        private readonly NotificationControlView notificationControlView;

        public NotificationControlPresenter(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            NotificationControlView notificationControlView) : base(stageNavigator, appState)
            => this.notificationControlView = notificationControlView;

        protected override void Initialize(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables)
        {
            appState.OnNotificationReceived
                .Subscribe(notificationControlView.Show)
                .AddTo(sceneDisposables);

            notificationControlView.OnOkButtonClicked
                .Subscribe(_ => notificationControlView.Hide())
                .AddTo(sceneDisposables);
        }
    }
}
