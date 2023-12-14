using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.App
{
    public class AppPresenter : DisposableBase, IInitializable, IAsyncStartable
    {
        private readonly StageNavigator<StageName, SceneName> stageNavigator;
        private readonly AppState appState;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public AppPresenter(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState)
        {
            this.stageNavigator = stageNavigator;
            this.appState = appState;
        }

        public void Initialize()
        {
            stageNavigator.OnStageTransitioned
                .Subscribe(appState.SetStage)
                .AddTo(disposables);

        }

        public async UniTask StartAsync(CancellationToken cancellation)
            => await stageNavigator.ReplaceAsync(StageName.GroupSelectionStage);

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
