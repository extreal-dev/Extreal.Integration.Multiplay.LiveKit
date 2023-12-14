using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS
{
    public interface IMultipayStrategy
    {
        void Initialize(Avatar avatar, bool isOwner, bool isTouchDevice);
        void ResetPosition();
        void DoLateUpdate();
    }
}
