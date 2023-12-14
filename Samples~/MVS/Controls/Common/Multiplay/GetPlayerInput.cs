using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public class GetPlayerInput : MonoBehaviour
    {
        [SerializeField] private HolidayPlayerInput input;

        private void Update()
            => input.SetInputFieldTyping(
                EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null);

        public void OnMove(InputValue value)
            => input.SetMove(value.Get<Vector2>());

        public void OnLook(InputValue value)
            => input.SetLook(value.Get<Vector2>());

        public void OnSprint(InputValue value)
            => input.SetSprint(value.isPressed);

        public void OnJump(InputValue value)
            => input.SetJump(value.isPressed);
    }
}
