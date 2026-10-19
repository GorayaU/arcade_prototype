using UnityEngine;
using UnityEngine.InputSystem;

/// First-person movement driven by the default InputSystem_Actions asset
/// (Move / Jump / Sprint on the "Player" action map).

/// Setup:
///   1. Player GameObject needs: CharacterController + PlayerInput + this script.
///   2. On PlayerInput, set Actions = InputSystem_Actions and Default Map = Player.
///   3. Child the camera under the player and add MouseLook to the player.

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Metres per second while walking.")]
    [SerializeField] private float walkSpeed = 5f;

    [Tooltip("Metres per second while sprinting (needs stamina).")]
    [SerializeField] private float sprintSpeed = 9f;

    [Tooltip("How fast the player reaches its target speed. Higher = snappier, less slidey.")]
    [SerializeField] private float acceleration = 14f;

    [Header("Jump & Gravity")]
    [Tooltip("Peak height of a jump, in metres.")]
    [SerializeField] private float jumpHeight = 1.4f;

    [Tooltip("Gravity. Real-world is -9.81, but games usually feel better heavier.")]
    [SerializeField] private float gravity = -25f;

    [Tooltip("Grace period after walking off a ledge where a jump still counts.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("A jump pressed this long before landing still fires on touchdown.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;

    [Tooltip("Stamina drained per second of sprinting.")]
    [SerializeField] private float sprintDrainPerSecond = 22f;

    [Tooltip("Stamina refilled per second once regen kicks in.")]
    [SerializeField] private float regenPerSecond = 16f;

    [Tooltip("Seconds after you stop sprinting before stamina starts refilling.")]
    [SerializeField] private float regenDelay = 1f;

    [Tooltip("Stamina spent per jump. Leave at 0 to make jumping free.")]
    [SerializeField] private float jumpStaminaCost = 0f;

    [Tooltip("After running dry, stamina must refill to this fraction before you can sprint again. " +
             "Stops the player stutter-sprinting on an empty bar.")]
    [Range(0f, 1f)][SerializeField] private float sprintUnlockThreshold = 0.25f;

    // --- Public read-only state (StaminaBar and anything else reads these) ---

    /// Current stamina as 0–1, for UI bars.
    public float StaminaNormalized => maxStamina <= 0f ? 0f : stamina / maxStamina;

    /// True while the player is actually sprinting (not just holding Shift).
    public bool IsSprinting { get; private set; }

    /// True after stamina hit zero, until it refills past the unlock threshold.
    public bool IsExhausted { get; private set; }

    /// True while the CharacterController is touching the ground.
    public bool IsGrounded { get; private set; }

    /// Current horizontal speed in metres per second.
    public float CurrentSpeed => horizontalVelocity.magnitude;

    // --- Internals ---

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float stamina;
    private float timeSinceSprintEnded;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina;

        // Pull the actions out of whatever asset PlayerInput is using, by name.
        // This works with the stock InputSystem_Actions asset without generating a C# class.
        var playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: false);
        jumpAction = playerInput.actions.FindAction("Jump", throwIfNotFound: false);
        sprintAction = playerInput.actions.FindAction("Sprint", throwIfNotFound: false);

        if (moveAction == null || jumpAction == null || sprintAction == null)
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
        float dt = Time.deltaTime;

        IsGrounded = controller.isGrounded;
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        UpdateStamina(moveInput, dt);
        UpdateHorizontalVelocity(moveInput, dt);
        UpdateVerticalVelocity(dt);

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
    }

    private void UpdateStamina(Vector2 moveInput, float dt)
    {
        // You only burn stamina if you're holding Sprint AND actually moving AND not gassed.
        bool wantsToSprint = sprintAction.IsPressed() && moveInput.sqrMagnitude > 0.01f;
        IsSprinting = wantsToSprint && !IsExhausted && stamina > 0f;

        if (IsSprinting)
        {
            stamina -= sprintDrainPerSecond * dt;
            timeSinceSprintEnded = 0f;

            if (stamina <= 0f)
            {
                stamina = 0f;
                IsExhausted = true;
                IsSprinting = false;
            }
        }
        else
        {
            timeSinceSprintEnded += dt;

            if (timeSinceSprintEnded >= regenDelay)
            {
                stamina = Mathf.Min(stamina + regenPerSecond * dt, maxStamina);
            }

            if (IsExhausted && StaminaNormalized >= sprintUnlockThreshold)
            {
                IsExhausted = false;
            }
        }
    }

    private void UpdateHorizontalVelocity(Vector2 moveInput, float dt)
    {
        // Movement is relative to where the player is facing, and MouseLook turns
        // the player body, so W always goes "where I'm looking".
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = direction * targetSpeed;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * dt);
    }

    private void UpdateVerticalVelocity(float dt)
    {
        // Coyote time: keep a jump available for a moment after leaving the ground.
        coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - dt;

        // Jump buffer: remember a press made slightly too early.
        jumpBufferTimer = jumpAction.WasPressedThisFrame() ? jumpBufferTime : jumpBufferTimer - dt;

        // Small downward bias so the controller stays glued to the floor
        // instead of flickering between grounded and airborne on slopes.
        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        bool canAffordJump = stamina >= jumpStaminaCost;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && canAffordJump)
        {
            // v = sqrt(2 * g * h) — the launch speed that peaks at exactly jumpHeight.
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (jumpStaminaCost > 0f)
            {
                stamina = Mathf.Max(0f, stamina - jumpStaminaCost);
                timeSinceSprintEnded = 0f;
                if (stamina <= 0f) IsExhausted = true;
            }

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        verticalVelocity += gravity * dt;

        // Bonked the ceiling — kill upward momentum so we don't hang there.
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }
}
