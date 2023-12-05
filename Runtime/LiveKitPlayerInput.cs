using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class RedisPlayerInput : MonoBehaviour
    {
        public virtual MultiplayPlayerInputValues Values => values;
        private MultiplayPlayerInputValues values;

        public void MoveInput(Vector2 newMoveDirection)
            => values.SetMove(newMoveDirection);

        public virtual void SetValues(MultiplayPlayerInputValues values)
            => MoveInput(values.Move);
    }
}
