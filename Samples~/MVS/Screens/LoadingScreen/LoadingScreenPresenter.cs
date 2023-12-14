using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.AssetWorkflow;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.App.Stages;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.LoadingScreen
{
    public class LoadingScreenPresenter : StagePresenterBase
    {
        private readonly LoadingScreenView loadingScreenView;
        private readonly AssetHelper assetHelper;

        public LoadingScreenPresenter
        (
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            LoadingScreenView loadingScreenView,
            AssetHelper assetHelper
        ) : base(stageNavigator, appState)
        {
            this.loadingScreenView = loadingScreenView;
            this.assetHelper = assetHelper;
        }

        protected override void Initialize(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables)
        {
            appState.PlayingReady
                .Subscribe(ready => loadingScreenView.SwitchVisibility(!ready))
                .AddTo(sceneDisposables);

            appState.OnNotificationReceived
                .Subscribe(_ => loadingScreenView.SwitchVisibility(false))
                .AddTo(sceneDisposables);

            assetHelper.OnDownloading
                .Subscribe(_ => loadingScreenView.SwitchVisibility(true))
                .AddTo(sceneDisposables);

            assetHelper.OnDownloaded
                .Subscribe(loadingScreenView.SetDownloadStatus)
                .AddTo(sceneDisposables);
        }

        protected override void OnStageEntered(
            StageName stageName,
            AppState appState,
            CompositeDisposable stageDisposables)
        {
            loadingScreenView.SwitchVisibility(false);
        }

        protected override void OnStageExiting(
            StageName stageName,
            AppState appState)
        {
            loadingScreenView.SwitchVisibility(true);
        }
    }
}
