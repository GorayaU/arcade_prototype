using UnityEngine;
using UnityEngine.InputSystem;

namespace Udey
{
    [RequireComponent(typeof(PlayerInput))]
    public class MouseLook : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = 0.2f;
        [SerializeField] private float gamepadSensitivity = 180f;
        [SerializeField] private bool invertY = false;

        [Header("Limits")]
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        private InputAction _lookAction;
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _lookAction = GetComponent<PlayerInput>().actions.FindAction("Look", throwIfNotFound: false);

            if (_lookAction == null)
            {
                Debug.LogError("[MouseLook] No 'Look' action found on the assigned Input Actions asset.", this);
                enabled = false;
                return;
            }

            if (cameraTransform == null)
            {
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
            _yaw = transform.eulerAngles.y;
            _pitch = 0f;

            if (lockCursor) SetCursorLocked(true);
        }

        private void Update()
        {
            HandleCursorToggle();

            if (lockCursor && Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 look = _lookAction.ReadValue<Vector2>();

            bool usingMouse = _lookAction.activeControl != null && _lookAction.activeControl.device is Pointer;
            float sensitivity = usingMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

            _yaw += look.x * sensitivity;
            _pitch += (invertY ? look.y : -look.y) * sensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

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
}