using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS
{
    public interface INetworkThirdPersonController
    {
        void Initialize(Avatar avatar, bool isOwner, bool isTouchDevice);
        void ResetPosition();
        void DoLateUpdate();
    }
}
