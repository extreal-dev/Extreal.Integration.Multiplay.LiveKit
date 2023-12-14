using Extreal.Integration.Multiplay.Common;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public class HolidayPlayerInput : MultiplayPlayerInput
    {
        public override MultiplayPlayerInputValues Values => HolidayValues;
        public HolidayPlayerInputValues HolidayValues { get; } = new HolidayPlayerInputValues();

        public Vector2 Look => look;
        [SerializeField] private Vector2 look;

        public override void SetMove(Vector2 newMoveDirection)
            => HolidayValues.SetMove(newMoveDirection);

        public void SetLook(Vector2 newLookDirection)
            => look = newLookDirection;

        public void SetSprint(bool newSprint)
            => HolidayValues.SetSprint(newSprint);

        public void SetJump(bool newJump)
            => HolidayValues.SetJump(newJump);

        public void SetInputFieldTyping(bool newValue)
            => HolidayValues.SetInputFieldTyping(newValue);

        public override void SetValues(MultiplayPlayerInputValues values)
        {
            var holidayValues = values as HolidayPlayerInputValues;

            base.SetValues(holidayValues);
            SetSprint(holidayValues.Sprint);
            SetJump(holidayValues.Jump);
            SetInputFieldTyping(holidayValues.InputFieldTyping);
        }
    }
}
