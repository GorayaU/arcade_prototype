using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    // Input actions
    [SerializeField] InputActionAsset InputActions;
    InputAction shoot;

    [SerializeField] GameObject bulletPrefab;
    [SerializeField] float bulletSpeed = 100.0f;

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
            Debug.Log("Shooting!");
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * bulletSpeed;
        }
    }
}
