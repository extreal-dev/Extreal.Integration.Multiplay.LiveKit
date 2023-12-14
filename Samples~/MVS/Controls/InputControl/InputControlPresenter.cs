using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.App.Stages;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.InputControl
{
    public class InputControlPresenter : StagePresenterBase
    {
        private readonly InputControlView inputControlView;

        public InputControlPresenter
        (
            InputControlView inputControlView,
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState
        ) : base(stageNavigator, appState)
            => this.inputControlView = inputControlView;

        protected override void OnStageEntered(StageName stageName, AppState appState, CompositeDisposable stageDisposables)
            => inputControlView.SwitchJoystickVisibility(AppUtils.IsSpace(stageName) && AppUtils.IsTouchDevice());
    }
}
