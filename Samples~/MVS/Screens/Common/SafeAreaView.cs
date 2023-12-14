using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.Common
{
    public class SafeAreaView : MonoBehaviour
    {
        [SuppressMessage("Style", "IDE0051")]
        private void Start()
        {
            var safeArea = Screen.safeArea;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMax.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.y /= Screen.height;

            var target = GetComponent<RectTransform>();
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
        }
    }
}
