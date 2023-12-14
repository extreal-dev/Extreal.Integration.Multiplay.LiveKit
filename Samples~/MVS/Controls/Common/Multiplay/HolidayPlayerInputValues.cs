using System;
using Extreal.Integration.Multiplay.Common;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    [Serializable]
    public class HolidayPlayerInputValues : MultiplayPlayerInputValues
    {
        private Vector2 preMove;
        private bool isMoveChanged;

        public bool Sprint => sprint;
        [SerializeField] private bool sprint;
        private bool preSprint;
        private bool isSprintChanged;

        public bool Jump => jump;
        [SerializeField] private bool jump;
        private bool preJump;
        private bool isJumpChanged;

        public bool InputFieldTyping => inputFieldTyping;
        [SerializeField] private bool inputFieldTyping;
        private bool preInputFieldTyping;
        private bool isInputFieldTypingChanged;

        public override void SetMove(Vector2 move)
        {
            preMove = Move;
            base.SetMove(move);
            isMoveChanged = preMove != Move;
        }

        public void SetSprint(bool sprint)
        {
            preSprint = this.sprint;
            this.sprint = sprint;
            isSprintChanged = preSprint != this.sprint;
        }

        public void SetJump(bool jump)
        {
            preJump = this.jump;
            this.jump = jump;
            isJumpChanged = preJump != this.jump;
        }

        public void SetInputFieldTyping(bool inputFieldTyping)
        {
            preInputFieldTyping = this.inputFieldTyping;
            this.inputFieldTyping = inputFieldTyping;
            isInputFieldTypingChanged = preInputFieldTyping != this.inputFieldTyping;
        }

        public override bool CheckWhetherToSendData()
        {
            var ret = isMoveChanged || isSprintChanged || isJumpChanged || isInputFieldTypingChanged;
            isMoveChanged = isSprintChanged = isJumpChanged = isInputFieldTypingChanged = false;
            return ret;
        }
    }
}
