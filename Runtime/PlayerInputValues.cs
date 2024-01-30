using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging
{
    /// <summary>
    /// Class that holds player input values.
    /// </summary>
    public class PlayerInputValues
    {
        /// <summary>
        /// Move direction to be input.
        /// </summary>
        public Vector2 Move { get; set; }

        /// <summary>
        /// Sets move value.
        /// </summary>
        /// <param name="move">Move direction to be set.</param>
        public virtual void SetMove(Vector2 move)
            => Move = move;

        /// <summary>
        /// Checks whether to send data to all other users.
        /// </summary>
        /// <returns>True if sending data, false otherwise.</returns>
        public virtual bool CheckWhetherToSendData()
            => true;
    }
}
