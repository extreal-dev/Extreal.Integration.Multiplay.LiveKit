using Cysharp.Threading.Tasks;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.App.Stages;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.ClientControl
{
    public class ClientControlPresenter : StagePresenterBase
    {
        private readonly MultiplayClient multiplayClient;

        public ClientControlPresenter(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            MultiplayClient multiplayClient) : base(stageNavigator, appState)
        {
            this.multiplayClient = multiplayClient;
        }

        protected override void Initialize(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables)
        {
            InitializeMultiplayClient(stageNavigator, appState, sceneDisposables);
        }



        private void InitializeMultiplayClient(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables)
        {
            multiplayClient.OnConnectionApprovalRejected
                .Subscribe(_ =>
                {
                    appState.Notify("Space is full.");
                    stageNavigator.ReplaceAsync(StageName.GroupSelectionStage).Forget();
                })
                .AddTo(sceneDisposables);

            multiplayClient.OnUnexpectedDisconnected
                .Subscribe(_ =>
                    appState.Notify("Multiplayer disconnected unexpectedly."))
                .AddTo(sceneDisposables);
        }
    }
}
