using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    // Input actions
    [SerializeField] InputActionAsset InputActions;
    InputAction shoot;

    // Shooting variables
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] float bulletSpeed = 100.0f;
    Vector2 screenPos;
    Vector3 mousePos;

    void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    void Awake()
    {
        shoot = InputSystem.actions.FindAction("Attack");
    }

    void Update()
    {
        if (shoot.WasPerformedThisFrame())
        {
            // Get the current position of the mouse cursor on the screen
            screenPos = Mouse.current.position.ReadValue();
            // Convert the screen position to world position based on the camera
            Vector3 camDis = new Vector3(screenPos.x, screenPos.y, 5f);
            mousePos = Camera.main.ScreenToWorldPoint(camDis);
            // Creates the bullet prefab where the cursor is positioned
            GameObject bullet = Instantiate(bulletPrefab, mousePos, Quaternion.identity);
            // Makes the bullet move forward relative to where the cursor is pointing
            bullet.GetComponent<Rigidbody>().linearVelocity = new Vector3(mousePos.normalized.x,
                mousePos.normalized.y, mousePos.normalized.z * -1) * bulletSpeed;
        }
    }
}
