using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.App.Stages;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.SpaceControl
{
    public class SpaceControlPresenter : StagePresenterBase
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(SpaceControlPresenter));

        private readonly SpaceControlView spaceControlView;

        public SpaceControlPresenter
        (
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            SpaceControlView spaceControlView
        ) : base(stageNavigator, appState)
        {
            this.spaceControlView = spaceControlView;
        }

        protected override void Initialize(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables)
        {
            spaceControlView.OnBackButtonClicked
                .Subscribe(_ => stageNavigator.ReplaceAsync(StageName.GroupSelectionStage).Forget())
                .AddTo(sceneDisposables);
        }
    }
}
