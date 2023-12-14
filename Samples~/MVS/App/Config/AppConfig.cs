using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.App.Config
{
    [CreateAssetMenu(
        menuName = nameof(MVS) + "/" + nameof(AppConfig),
        fileName = nameof(AppConfig))]
    public class AppConfig : ScriptableObject
    {
        [SerializeField] private int verticalSyncs;
        [SerializeField] private int targetFrameRate;
        [SerializeField] private int downloadTimeoutSeconds;
        [SerializeField] private int downloadMaxRetryCount;
        [SerializeField] private string downloadConnectRetryMessage;
        [SerializeField] private string downloadRetrySuccessMessage;
        [SerializeField] private string downloadRetryFailureMessage;

        public int VerticalSyncs => verticalSyncs;
        public int TargetFrameRate => targetFrameRate;
        public int DownloadTimeoutSeconds => downloadTimeoutSeconds;
        public int DownloadMaxRetryCount => downloadMaxRetryCount;
        public string DownloadConnectRetryMessage => downloadConnectRetryMessage;
        public string DownloadRetrySuccessMessage => downloadRetrySuccessMessage;
        public string DownloadRetryFailureMessage => downloadRetryFailureMessage;
    }
}
