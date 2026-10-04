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
        // Repeatedly calls the function to shoot after the attack input is performed
        if (shoot.WasPerformedThisFrame())
        {
            InvokeRepeating("ShootProjectile", 0f, 0.5f);
        }
        // After the attack input is released, the shoot function will stop being called
        else if (shoot.WasReleasedThisFrame())
        {
            CancelInvoke();
        }
    }

    void ShootProjectile()
    {
        // Shoots in the direction of the camera
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().linearVelocity = cameraTransform.forward * bulletSpeed;
    }
}
