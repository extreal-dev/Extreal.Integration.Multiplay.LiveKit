using System;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    [Serializable]
    public class ExtrealPlayerInputValues
    {
        public Vector2 Move => move;
        [SerializeField] private Vector2 move;

        public void SetMove(Vector2 move)
            => this.move = move;
    }
}
