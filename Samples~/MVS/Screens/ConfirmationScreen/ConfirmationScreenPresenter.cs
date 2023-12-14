using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.App.Stages;
using UniRx;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.ConfirmationScreen
{
    public class ConfirmationScreenPresenter : StagePresenterBase
    {
        private readonly ConfirmationScreenView confirmationScreenView;

        public ConfirmationScreenPresenter
        (
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            ConfirmationScreenView confirmationScreenView
        ) : base(stageNavigator, appState)
            => this.confirmationScreenView = confirmationScreenView;

        protected override void Initialize(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables)
        {
            var confirmation = default(Confirmation);

            appState.OnConfirmationReceived
                .Subscribe(c =>
                {
                    confirmation = c;
                    confirmationScreenView.Show(c.Message);
                })
                .AddTo(sceneDisposables);

            confirmationScreenView.OkButtonClicked
                .Subscribe(_ =>
                {
                    confirmationScreenView.Hide();
                    confirmation.OkAction?.Invoke();
                })
                .AddTo(sceneDisposables);

            confirmationScreenView.CancelButtonClicked
                .Subscribe(_ =>
                {
                    confirmationScreenView.Hide();
                    confirmation.CancelAction?.Invoke();
                })
                .AddTo(sceneDisposables);
        }
    }
}
