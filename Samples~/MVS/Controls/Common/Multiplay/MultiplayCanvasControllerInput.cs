using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public class MultiplayCanvasControllerInput : MonoBehaviour
    {
        public HolidayPlayerInput HolidayPlayerInput => holidayPlayerInput;
        [SerializeField] private HolidayPlayerInput holidayPlayerInput;

        public void VirtualMoveInput(Vector2 virtualMoveDirection) => holidayPlayerInput.SetMove(virtualMoveDirection);

        public void VirtualLookInput(Vector2 virtualLookDirection) => holidayPlayerInput.SetLook(virtualLookDirection);

        public void VirtualJumpInput(bool virtualJumpState) => holidayPlayerInput.SetJump(virtualJumpState);

        public void VirtualSprintInput(bool virtualSprintState) => holidayPlayerInput.SetSprint(virtualSprintState);

        public void SetHolidayPlayerInput(HolidayPlayerInput holidayPlayerInput) => this.holidayPlayerInput = holidayPlayerInput;
    }
}
