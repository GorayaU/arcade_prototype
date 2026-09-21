using UnityEngine;
using UnityEngine.InputSystem;

namespace Udey
{
    /// Setup:
    ///   1. Player GameObject needs: CharacterController + PlayerInput + this script.
    ///   2. On PlayerInput, set Actions = InputSystem_Actions and Default Map = Player.
    ///   3. Child the camera under the player and add MouseLook to the player.

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 9f;
        [SerializeField] private float acceleration = 14f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float coyoteTime = 0.12f; //Grace period after walking off a ledge where a jump still counts
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float sprintDrainPerSecond = 22f;
        [SerializeField] private float regenPerSecond = 16f;
        [SerializeField] private float regenDelay = 1f;
        [SerializeField] private float jumpStaminaCost = 0f;
        [Range(0f, 1f)][SerializeField] private float sprintUnlockThreshold = 0.25f;

        public float StaminaNormalized => maxStamina <= 0f ? 0f : _stamina / maxStamina;
        public bool IsSprinting { get; private set; }
        public bool IsExhausted { get; private set; }
        public bool IsGrounded { get; private set; }
        public float CurrentSpeed => _horizontalVelocity.magnitude;

        private CharacterController _controller;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _stamina;
        private float _timeSinceSprintEnded;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stamina = maxStamina;

            var playerInput = GetComponent<PlayerInput>();
            _moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: false);
            _jumpAction = playerInput.actions.FindAction("Jump", throwIfNotFound: false);
            _sprintAction = playerInput.actions.FindAction("Sprint", throwIfNotFound: false);

            if (_moveAction == null || _jumpAction == null || _sprintAction == null)
            {
                Debug.LogError(
                    "[PlayerMovement] Couldn't find Move / Jump / Sprint on the assigned Input Actions asset. " +
                    "Check that PlayerInput has InputSystem_Actions assigned and Default Map is set to 'Player'.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;

            IsGrounded = _controller.isGrounded;
            var moveInput = _moveAction.ReadValue<Vector2>();

            UpdateStamina(moveInput, dt);
            UpdateHorizontalVelocity(moveInput, dt);
            UpdateVerticalVelocity(dt);

            _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
        }

        private void UpdateStamina(Vector2 moveInput, float dt)
        {
            bool wantsToSprint = _sprintAction.IsPressed() && moveInput.sqrMagnitude > 0.01f;
            IsSprinting = wantsToSprint && !IsExhausted && _stamina > 0f;

            if (IsSprinting)
            {
                _stamina -= sprintDrainPerSecond * dt;
                _timeSinceSprintEnded = 0f;

                if (!(_stamina <= 0f)) return;
                _stamina = 0f;
                IsExhausted = true;
                IsSprinting = false;
            }
            else
            {
                _timeSinceSprintEnded += dt;

                if (_timeSinceSprintEnded >= regenDelay)
                {
                    _stamina = Mathf.Min(_stamina + regenPerSecond * dt, maxStamina);
                }

                if (IsExhausted && StaminaNormalized >= sprintUnlockThreshold)
                {
                    IsExhausted = false;
                }
            }
        }

        private void UpdateHorizontalVelocity(Vector2 moveInput, float dt)
        {
            Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = direction * targetSpeed;

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, acceleration * dt);
        }

        private void UpdateVerticalVelocity(float dt)
        {
            _coyoteTimer = IsGrounded ? coyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer = _jumpAction.WasPressedThisFrame() ? jumpBufferTime : _jumpBufferTimer - dt;

            if (IsGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            bool canAffordJump = _stamina >= jumpStaminaCost;
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && canAffordJump)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (jumpStaminaCost > 0f)
                {
                    _stamina = Mathf.Max(0f, _stamina - jumpStaminaCost);
                    _timeSinceSprintEnded = 0f;
                    if (_stamina <= 0f) IsExhausted = true;
                }

                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }

            _verticalVelocity += gravity * dt;

            if ((_controller.collisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }
        }
    }
}