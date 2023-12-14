using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
#pragma warning disable
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class RedisThirdPersonController : MonoBehaviour, IMultipayStrategy
    {
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        private CharacterController _controller;

        public NetworkVariable<NetworkString> AvatarAssetName { get; set; }
            = new NetworkVariable<NetworkString>(writePerm: NetworkVariableWritePermission.Owner);

        private MultiplayStrategyBase multiplayStrategy;

        private void LateUpdate()
        {
            DoLateUpdate();
        }

        public void Initialize(Avatar avatar, bool isOwner, bool isTouchDevice)
        {
            multiplayStrategy = new RedisMultiplayStrategy(gameObject, CinemachineCameraTarget, GroundLayers);
            _controller = GetComponent<CharacterController>();

            multiplayStrategy.Initialize(avatar, isOwner, isTouchDevice);

            enabled = true;
        }

        public void ResetPosition() => multiplayStrategy.ResetPosition();
        public void DoLateUpdate() => multiplayStrategy.DoLateUpdate();

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (!enabled)
            {
                return;
            }

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center),
                        FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (!enabled)
            {
                return;
            }

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
            }
        }

    }
}
