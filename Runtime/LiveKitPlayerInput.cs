using UnityEngine;
using UnityEngine.InputSystem;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class LiveKitPlayerInput : MonoBehaviour
    {
        public virtual LiveKitPlayerInputValues Values => values;
        private LiveKitPlayerInputValues values;

        public Vector2 Look => look;
        private Vector2 look;

        public void OnMove(InputValue value)
            => MoveInput(value.Get<Vector2>());

        public void OnLook(InputValue value)
            => LookInput(value.Get<Vector2>());

        public void MoveInput(Vector2 newMoveDirection)
            => values.SetMove(newMoveDirection);

        public void LookInput(Vector2 newLookDirection)
            => look = newLookDirection;

        public virtual void SetValues(LiveKitPlayerInputValues values)
            => MoveInput(values.Move);
    }
}
