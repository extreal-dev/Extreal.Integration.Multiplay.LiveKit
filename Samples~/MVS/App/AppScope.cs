using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Logging;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App.AssetWorkflow;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.App
{
    public class AppScope : LifetimeScope
    {
        [SerializeField] private AppConfig appConfig;
        [SerializeField] private LoggingConfig loggingConfig;
        [SerializeField] private StageConfig stageConfig;

        private void InitializeApp()
        {
            QualitySettings.vSyncCount = appConfig.VerticalSyncs;
            Application.targetFrameRate = appConfig.TargetFrameRate;
            var timeout = appConfig.DownloadTimeoutSeconds;
            Addressables.ResourceManager.WebRequestOverride = unityWebRequest => unityWebRequest.timeout = timeout;

            ClearCacheOnDev();

            var logLevel = InitializeLogging();
            InitializeWebGL();

            var logger = LoggingManager.GetLogger(nameof(AppScope));
            if (logger.IsDebug())
            {
                logger.LogDebug(
                    $"targetFrameRate: {Application.targetFrameRate}, unityWebRequest.timeout: {timeout}, logLevel: {logLevel}");
            }
        }

        private LogLevel InitializeLogging()
        {

            const LogLevel logLevel = LogLevel.Debug;
            var checker = new LogLevelLogOutputChecker(loggingConfig.CategoryFilters);
            var defaultWriter = new UnityDebugLogWriter(loggingConfig.LogFormats);
            LoggingManager.Initialize(logLevel, checker, defaultWriter);
            return logLevel;
        }

        private static void InitializeWebGL()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Extreal.Integration.Web.Common.WebGLHelper.Initialize();
#endif
        }

        private readonly AppStateProvider appStateProvider = new AppStateProvider();

        // The provider is added to pass AppState to LogWriter. AppState gets the logger to output logs.
        // Therefore, if AppState is created before log output is initialized,
        // only AppState acquires the logger before initialization, resulting in inconsistency.
        // In order to resolve this issue, the provider is introduced and AppState is passed to LogWriter
        // while delaying the timing of AppState creation.
        public class AppStateProvider
        {
            public AppState AppState { get; private set; }
            internal AppStateProvider() { }
            internal void Init() => AppState = new AppState();
        }

        [SuppressMessage("Design", "IDE0022"), SuppressMessage("Design", "CC0091")]
        private void ClearCacheOnDev()
        {
#if !HOLIDAY_PROD && ENABLE_CACHING
            Caching.ClearCache();
#endif
        }

        protected override void Awake()
        {
            InitializeApp();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(appConfig);

            builder.RegisterComponent(stageConfig).AsImplementedInterfaces();
            builder.Register<StageNavigator<StageName, SceneName>>(Lifetime.Singleton);

            appStateProvider.Init();
            builder.RegisterComponent(appStateProvider.AppState);

            builder.Register<AssetHelper>(Lifetime.Singleton);

            builder.RegisterEntryPoint<AppPresenter>();
        }
    }
}
