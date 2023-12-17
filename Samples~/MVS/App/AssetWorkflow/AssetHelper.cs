using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.Retry;
using Extreal.Core.Common.System;
using Extreal.Core.Logging;
using Extreal.Core.StageNavigation;
using Extreal.Integration.AssetWorkflow.Addressables;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using UniRx;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Extreal.Integration.Multiplay.Common.MVS.App.AssetWorkflow
{
    public class AssetHelper : DisposableBase
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(AssetHelper));

        private readonly StageNavigator<StageName, SceneName> stageNavigator;
        private readonly AssetProvider assetProvider;
        private readonly AppState appState;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable assetDisposables = new CompositeDisposable();

        [SuppressMessage("Usage", "CC0022")]
        public AssetHelper(
            StageNavigator<StageName, SceneName> stageNavigator, AppState appState)
        {
            this.stageNavigator = stageNavigator;
            this.appState = appState;
            assetProvider = new AssetProvider(new CountingRetryStrategy());
        }

        protected override void ReleaseManagedResources()
        {
            assetDisposables.Dispose();
            assetProvider.Dispose();
        }

        public UniTask<AssetDisposable<T>> LoadAssetAsync<T>(string assetName)
            => assetProvider.LoadAssetAsync<T>(assetName);

        public UniTask<AssetDisposable<SceneInstance>> LoadSceneAsync(string assetName)
            => assetProvider.LoadSceneAsync(assetName);
    }
}
