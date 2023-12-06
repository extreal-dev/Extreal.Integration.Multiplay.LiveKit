using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public class RedisPlayerInput : MonoBehaviour
    {
        public virtual MultiplayPlayerInputValues Values => values;
        private MultiplayPlayerInputValues values;

        public virtual void SetMove(Vector2 newMoveDirection)
            => values.SetMove(newMoveDirection);

        public virtual void SetValues(MultiplayPlayerInputValues values)
            => SetMove(values.Move);
    }
}
