using UnityEngine;
using UnityEngine.InputSystem;

/// First-person camera look driven by the "Look" action on the default
/// InputSystem_Actions asset. Goes on the same GameObject as PlayerMovement:
/// it yaws the player body (so WASD follows the camera) and pitches the camera only.

[RequireComponent(typeof(PlayerInput))]
public class MouseLook : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera, as a child of this GameObject. Roughly at head height.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Sensitivity")]
    [Tooltip("Degrees of rotation per pixel of mouse movement.")]
    [SerializeField] private float mouseSensitivity = 0.2f;

    [Tooltip("Degrees per second at full gamepad stick deflection.")]
    [SerializeField] private float gamepadSensitivity = 180f;

    [SerializeField] private bool invertY = false;

    [Header("Limits")]
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Cursor")]
    [Tooltip("Lock and hide the cursor on start. Press Escape to release it in the editor.")]
    [SerializeField] private bool lockCursor = true;

    private InputAction lookAction;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        lookAction = GetComponent<PlayerInput>().actions.FindAction("Look", throwIfNotFound: false);

        if (lookAction == null)
        {
            Debug.LogError("[MouseLook] No 'Look' action found on the assigned Input Actions asset.", this);
            enabled = false;
            return;
        }

        if (cameraTransform == null)
        {
            // Fall back to a child camera if one wasn't assigned in the inspector.
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null) cameraTransform = childCamera.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("[MouseLook] No camera assigned and none found in children.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = 0f;

        if (lockCursor) SetCursorLocked(true);
    }

    private void Update()
    {
        HandleCursorToggle();

        // Don't drag the camera around while the cursor is free (paused, clicking in the editor).
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        // Mouse delta is already per-frame movement, so it must NOT be multiplied by
        // deltaTime. A gamepad stick is a held position, so it must be. Mixing these
        // up is why gamepad look usually feels broken.
        bool usingMouse = lookAction.activeControl != null && lookAction.activeControl.device is Pointer;
        float sensitivity = usingMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

        yaw += look.x * sensitivity;
        pitch += (invertY ? look.y : -look.y) * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// Escape frees the cursor (so you can hit Pause in the editor); clicking re-locks it.
    private void HandleCursorToggle()
    {
        if (!lockCursor) return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
        }
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
