using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.BackgroundScreen
{
    public class BackgroundScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject screen;

        public void Show() => screen.SetActive(true);

        public void Hide() => screen.SetActive(false);
    }
}
