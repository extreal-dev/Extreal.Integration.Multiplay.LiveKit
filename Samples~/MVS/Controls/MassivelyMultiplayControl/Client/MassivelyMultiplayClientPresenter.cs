using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Messaging.Common;
using Extreal.Integration.Multiplay.Common;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.AssetWorkflow;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.App.Stages;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.MassivelyMultiplyControl.Client
{
    public class MassivelyMultiplayClientPresenter : StagePresenterBase
    {
        private readonly MultiplayClient multiplayClient;
        private readonly GroupManager groupManager;
        private readonly AssetHelper assetHelper;
        private MassivelyMultiplayClient massivelyMultiplayClient;

        public MassivelyMultiplayClientPresenter
        (
            MultiplayClient multiplayClient,
            GroupManager groupManager,
            AssetHelper assetHelper,
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState
        ) : base(stageNavigator, appState)
        {
            this.multiplayClient = multiplayClient;
            this.groupManager = groupManager;
            this.assetHelper = assetHelper;
        }

        [SuppressMessage("CodeCracker", "CC0092")]
        protected override void Initialize
        (
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables
        )
        {
            if (!appState.IsMassivelyForCommunication)
            {
                return;
            }

            massivelyMultiplayClient = new MassivelyMultiplayClient(multiplayClient, groupManager, assetHelper, appState);
            sceneDisposables.Add(massivelyMultiplayClient);

            massivelyMultiplayClient.IsPlayerSpawned
                .Subscribe(appState.SetMultiplayReady)
                .AddTo(sceneDisposables);

            appState.SpaceReady
                .Where(ready => ready && !appState.MultiplayReady.Value)
                .Subscribe(_ => massivelyMultiplayClient.JoinAsync().Forget())
                .AddTo(sceneDisposables);

            appState.PlayingReady
                .Skip(1)
                .Where(ready => ready)
                .Subscribe(_ => massivelyMultiplayClient.ResetPosition())
                .AddTo(sceneDisposables);
        }

        protected override void OnStageExiting(StageName stageName, AppState appState)
        {
            if (!appState.IsMassivelyForCommunication || AppUtils.IsSpace(stageName))
            {
                return;
            }
            appState.SetMultiplayReady(false);
            massivelyMultiplayClient.LeaveAsync().Forget();
        }
    }
}
