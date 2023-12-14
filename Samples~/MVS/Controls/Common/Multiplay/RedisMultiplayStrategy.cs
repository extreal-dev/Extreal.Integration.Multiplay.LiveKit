using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public class RedisMultiplayStrategy : MultiplayStrategyBase
    {
        public RedisMultiplayStrategy(GameObject player, GameObject cinemachineCameraTarget, LayerMask groundLayers) : base(player, cinemachineCameraTarget, groundLayers)
        {
        }

        public override void DoLateUpdate()
        {
            if (isOwner)
            {
                GroundedCheck();

                if (!Input.HolidayValues.InputFieldTyping)
                {
                    JumpAndGravity(true);
                    Move(true);

                    if (!isTouchDevice)
                    {
                        MouseCameraRotation();
                    }
                    else
                    {
                        TouchDeviceCameraRotation();
                    }
                }
                else
                {
                    JumpAndGravity(false);
                    Move(false);
                }
            }
            else
            {
                GroundedCheck();

                if (!Input.HolidayValues.InputFieldTyping)
                {
                    JumpAndGravity(true);
                    Move(true);
                }
                else
                {
                    JumpAndGravity(false);
                    Move(false);
                }
            }
        }
    }
}
