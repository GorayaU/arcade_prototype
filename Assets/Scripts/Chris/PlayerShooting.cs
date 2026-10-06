using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    // Input actions
    [SerializeField] InputActionAsset InputActions;
    InputAction shoot;
    InputAction reload;

    // Shooting variables
    [Header("Shooting")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] float bulletSpeed = 80.0f;
    [SerializeField] float fireRate = 0.35f;
    [SerializeField] float reloadSpeed = 0.8f;
    bool reloading;
    [SerializeField] int maxAmmo = 12;
    int currentAmmo;
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
        reload = InputSystem.actions.FindAction("Reload");
        reloading = false;
        currentAmmo = maxAmmo;
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Repeatedly calls the function to shoot after the attack input is performed
        if (shoot.WasPerformedThisFrame())
        {
            InvokeRepeating("ShootProjectile", 0f, fireRate);
        }
        // After the attack input is released, the shoot function will stop being called
        else if (shoot.WasReleasedThisFrame())
        {
            CancelInvoke();
        }

        // Starts reloading the player's weapon if their ammo isn't full
        if (reload.WasPerformedThisFrame() && currentAmmo != maxAmmo)
        {
            StartCoroutine(ReloadTimer());
        }
    }

    void ShootProjectile()
    {
        if (currentAmmo > 0 && !reloading)
        {
            // Shoots in the direction of the camera
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.GetComponent<Rigidbody>().linearVelocity = cameraTransform.forward * bulletSpeed;

            currentAmmo--;
            Debug.Log(currentAmmo);
        }
    }

    IEnumerator ReloadTimer()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadSpeed);
        currentAmmo = maxAmmo;
        Debug.Log(currentAmmo);
        reloading = false;
    }
}
