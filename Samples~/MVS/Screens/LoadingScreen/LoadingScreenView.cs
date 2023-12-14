using System;
using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Logging;
using Extreal.Integration.AssetWorkflow.Addressables;
using Extreal.Integration.Multiplay.Common.MVS.App;
using TMPro;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.LoadingScreen
{
    public class LoadingScreenView : MonoBehaviour
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(LoadingScreenView));

        [SerializeField] private GameObject screen;
        [SerializeField] private TMP_Text loadedPercent;

        [SuppressMessage("Style", "IDE0051")]
        private void Start() => screen.SetActive(false);

        public void SwitchVisibility(bool isVisible)
        {
            if (Logger.IsDebug())
            {
                var status = isVisible ? "ON" : "OFF";
                Logger.LogDebug($"Loading: {status}");
            }
            if (!isVisible)
            {
                ClearLoadedPercent();
            }
            screen.SetActive(isVisible);
        }

        public void SetDownloadStatus(AssetDownloadStatus downloadStatus)
        {
            if (downloadStatus.Status.TotalBytes == 0L)
            {
                return;
            }

            var total = AppUtils.GetSizeUnit(downloadStatus.Status.TotalBytes);
            var downloaded = AppUtils.GetSizeUnit(downloadStatus.Status.DownloadedBytes);
            loadedPercent.text = $"{downloadStatus.Status.Percent * 100:F0}%" +
                                 Environment.NewLine +
                                 $"({downloaded.Item1}{downloaded.Item2}/{total.Item1}{total.Item2})";
        }

        private void ClearLoadedPercent()
            => loadedPercent.text = string.Empty;
    }
}
