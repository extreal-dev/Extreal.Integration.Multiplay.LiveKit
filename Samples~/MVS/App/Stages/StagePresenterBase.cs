using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Common.System;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.App.Stages
{
    public abstract class StagePresenterBase : DisposableBase, IInitializable
    {
        private readonly StageNavigator<StageName, SceneName> stageNavigator;
        private readonly AppState appState;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable sceneDisposables = new CompositeDisposable();

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable stageDisposables = new CompositeDisposable();

        protected StagePresenterBase(StageNavigator<StageName, SceneName> stageNavigator, AppState appState)
        {
            this.stageNavigator = stageNavigator;
            this.appState = appState;
        }

        public void Initialize()
        {
            stageNavigator.OnStageTransitioned
                .Subscribe(stageName => OnStageEntered(stageName, appState, stageDisposables))
                .AddTo(sceneDisposables);

            stageNavigator.OnStageTransitioning
                .Subscribe(stageName =>
                {
                    OnStageExiting(stageName, appState);
                    stageDisposables.Clear();
                })
                .AddTo(sceneDisposables);

            Initialize(stageNavigator, appState, sceneDisposables);
        }

        protected virtual void Initialize
        (
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            CompositeDisposable sceneDisposables
        )
        {
        }

        protected virtual void OnStageEntered
        (
            StageName stageName,
            AppState appState,
            CompositeDisposable stageDisposables
        )
        {
        }

        protected virtual void OnStageExiting(StageName stageName, AppState appState)
        {
        }

        protected override void ReleaseManagedResources()
        {
            stageDisposables.Dispose();
            sceneDisposables.Dispose();
        }
    }
}
