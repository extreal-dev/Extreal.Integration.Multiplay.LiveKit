using System;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    [Serializable]
    public class MultiplayPlayerInputValues
    {
        public Vector2 Move => move;
        [SerializeField] private Vector2 move;

        public void SetMove(Vector2 move)
            => this.move = move;
    }
}
