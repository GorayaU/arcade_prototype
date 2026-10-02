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
    Transform cameraTransform;

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
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (shoot.WasPerformedThisFrame())
        {
            // Shoots in the direction of the camera
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.GetComponent<Rigidbody>().linearVelocity = cameraTransform.forward * bulletSpeed;
        }
    }
}
