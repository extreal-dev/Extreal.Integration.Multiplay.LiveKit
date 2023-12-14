using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public abstract class MultiplayStrategyBase : IMultipayStrategy
    {
        private readonly GameObject player;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        private const float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        private const float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        private const float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        private const float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        private const float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        private const float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        private const float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        private const float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        private bool Grounded = true;

        [Tooltip("Useful for rough ground")] private const float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        private const float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        private LayerMask groundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        private GameObject cinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        private const float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        private const float BottomClamp = -30.0f;

        [Tooltip("Additional degrees to override the camera. Useful for fine tuning camera position when locked")]
        private const float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        private const bool LockCameraPosition = false;

        [Header("Input")] private PlayerInput PlayerInput;
        private GetPlayerInput GetPlayerInput;
        protected HolidayPlayerInput Input;

        private const float cameraRotateSpeed = 10.0f;
        private const float dampingFactor = 0.1f;
        private float yawDelta = 0.0f;
        private float pitchDelta = 0.0f;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout delta time
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private CinemachineVirtualCamera _cinemachineVirtualCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        protected bool isTouchDevice;
        protected bool isOwner;
        private const bool isClient = true;
        private bool isNetcode;

        protected MultiplayStrategyBase(GameObject player, GameObject cinemachineCameraTarget, LayerMask groundLayers)
        {
            this.player = player;
            this.cinemachineCameraTarget = cinemachineCameraTarget;
            this.groundLayers = groundLayers;

            PlayerInput = player.GetComponent<PlayerInput>();
            GetPlayerInput = player.GetComponent<GetPlayerInput>();
            Input = player.GetComponent<HolidayPlayerInput>();
        }

        public void Initialize(Avatar avatar, bool isOwner, bool isTouchDevice)
        {
            this.isOwner = isOwner;

            _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = player.TryGetComponent(out _animator);
            _controller = player.GetComponent<CharacterController>();

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            SetAvatar(avatar);
            SetOwnerCamera();

            if (isOwner && isTouchDevice)
            {
                RegisterCurrentDeviceIsTouchDevice();
            }
        }

        private void SetAvatar(Avatar avatar)
        {
            if (!_hasAnimator)
            {
                _hasAnimator = player.TryGetComponent(out _animator);
            }

            var speed = _animator.GetFloat(_animIDSpeed);
            var grounded = _animator.GetBool(_animIDGrounded);
            var jump = _animator.GetBool(_animIDJump);
            var freeFall = _animator.GetBool(_animIDFreeFall);
            var motionSpeed = _animator.GetFloat(_animIDMotionSpeed);

            _animator.avatar = avatar;

            _animator.SetFloat(_animIDSpeed, speed);
            _animator.SetBool(_animIDGrounded, grounded);
            _animator.SetBool(_animIDJump, jump);
            _animator.SetBool(_animIDFreeFall, freeFall);
            _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }

        private void RegisterCurrentDeviceIsTouchDevice()
        {
            isTouchDevice = true;
            var multiplayCanvasControllerInput = Object.FindObjectOfType<MultiplayCanvasControllerInput>();
            multiplayCanvasControllerInput.SetHolidayPlayerInput(Input);
            PlayerInput.neverAutoSwitchControlSchemes = true;
        }

        public void ResetPosition()
        {
            _controller.enabled = false;
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;
            _cinemachineTargetYaw = 0f;
            _cinemachineTargetPitch = 0f;
            _controller.enabled = true;
        }

        private void SetOwnerCamera()
        {
            if (isClient && isOwner)
            {
                _cinemachineVirtualCamera = Object.FindObjectOfType<CinemachineVirtualCamera>();

                PlayerInput.enabled = true;
                GetPlayerInput.enabled = true;
                _cinemachineVirtualCamera.Follow = cinemachineCameraTarget.transform;
            }
        }

        public abstract void DoLateUpdate();

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        protected void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(player.transform.position.x, player.transform.position.y - GroundedOffset,
                player.transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, groundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        protected void TouchDeviceCameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (Input.Look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = Time.deltaTime;
                _cinemachineTargetYaw += Input.Look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += Input.Look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        protected void MouseCameraRotation()
        {
            var mouse = Mouse.current;
            // if there is an input and camera position is not fixed
            if (Input.Look.sqrMagnitude >= _threshold && !LockCameraPosition && mouse.leftButton.isPressed)
            {
                yawDelta += Input.Look.x * cameraRotateSpeed;
                pitchDelta += Input.Look.y * cameraRotateSpeed;
            }

            _cinemachineTargetYaw += yawDelta * dampingFactor;
            _cinemachineTargetPitch += pitchDelta * dampingFactor;

            yawDelta *= 1f - dampingFactor;
            pitchDelta *= 1f - dampingFactor;

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        protected void Move(bool isMovable)
        {
            var move = isMovable ? Input.HolidayValues.Move : Vector2.zero;

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = Input.HolidayValues.Sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (move == Vector2.zero)
                targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f)
                _animationBlend = 0f;

            // normalize input direction
            Vector3 inputDirection = new Vector3(move.x, 0.0f, move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _cinemachineTargetYaw;
                float rotation = Mathf.SmoothDampAngle(player.transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                if (isOwner)
                {
                    // rotate to face input direction relative to camera position
                    player.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            if (isOwner)
            {
                // move the player
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, 1f);
            }
        }

        protected void JumpAndGravity(bool isJumpable)
        {
            var jump = isJumpable && Input.HolidayValues.Jump;
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            Input.SetJump(false);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f)
                lfAngle += 360f;
            if (lfAngle > 360f)
                lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}
