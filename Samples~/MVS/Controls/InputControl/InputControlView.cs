using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.InputControl
{
    public class InputControlView : MonoBehaviour
    {
        [SerializeField] private GameObject joystickCanvas;

        public void SwitchJoystickVisibility(bool isVisible)
            => joystickCanvas.SetActive(isVisible);
    }
}
