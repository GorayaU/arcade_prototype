using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    // Input actions
    [SerializeField] InputActionAsset InputActions;
    InputAction shoot;
    InputAction reload;
    InputAction switchWeapon;

    // Shooting variables
    [Header("Shooting")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] float bulletSpeed = 80.0f;
    string weaponName = "Pistol";
    [SerializeField] float fireRate = 0.35f;
    bool attacking;
    [SerializeField] float reloadSpeed = 0.8f;
    bool reloading;
    [SerializeField] float weaponDamage = 15f;
    [SerializeField] int maxAmmo = 12;
    int currentAmmo;
    Transform cameraTransform;

    // Weapon switching variables
    WeaponStats weapon1Data;
    WeaponStats weapon2Data;
    [SerializeField] float switchSpeed = 0.6f;
    bool weapon1Equipped;
    bool switching;

    // UI variables
    [Header("UI")]
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] TextMeshProUGUI weaponNameText;

    void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    public float GetWeaponDamage()
    {
        return weaponDamage;
    }

    void Awake()
    {
        shoot = InputSystem.actions.FindAction("Attack");
        reload = InputSystem.actions.FindAction("Reload");
        switchWeapon = InputSystem.actions.FindAction("Switch Weapon");
        attacking = false;
        reloading = false;
        cameraTransform = Camera.main.transform;

        // Assigning the player's weapons
        weapon1Data = GetComponent<CurrentWeapon>().GetPistolData();
        weapon2Data = GetComponent<CurrentWeapon>().GetARifleData();
        SwitchWeapon(weapon1Data);
        weapon1Equipped = true;
        switching = false;

        // Updating UI information
        ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
        weaponNameText.text = weaponName;
    }

    void Update()
    {
        // Repeatedly calls the function to shoot after the attack input is performed
        if (shoot.WasPerformedThisFrame() && !attacking)
        {
            InvokeRepeating("ShootProjectile", 0f, fireRate);
            // Coroutine prevents the player from spamming the shoot input to fire faster
            StartCoroutine(ShootWaitTime());
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

        if (switchWeapon.WasPerformedThisFrame())
        {
            StartCoroutine(SwitchWeaponDelay());
            switching = true;
        }
    }

    void ShootProjectile()
    {
        if (currentAmmo > 0 && !reloading && !switching)
        {
            // Shoots in the direction of the camera
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.GetComponent<Rigidbody>().linearVelocity = cameraTransform.forward * bulletSpeed;

            // Updates the player's ammo
            currentAmmo--;
            ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";

            StartCoroutine(CameraShakeTime());
        }
    }

    void SwitchWeapon(WeaponStats newStats)
    {
        weaponName = newStats.name;
        fireRate = newStats.fireRate;
        reloadSpeed = newStats.reloadSpeed;
        weaponDamage = newStats.damage;
        maxAmmo = newStats.maxAmmo;
        weaponNameText.text = weaponName;
        currentAmmo = maxAmmo;
    }

    IEnumerator ShootWaitTime()
    {
        attacking = true;
        yield return new WaitForSeconds(fireRate);
        attacking = false;
    }

    IEnumerator CameraShakeTime()
    {
        if (GetComponentInChildren<CameraShake>() != null)
        {
            // Triggers a shake effect for a short time
            GetComponentInChildren<CameraShake>().enabled = true;
            yield return new WaitForSeconds(0.15f);
            GetComponentInChildren<CameraShake>().enabled = false;
        }
        else
        {
            Debug.Log("No camera shake script detected!");
            yield return null;
        }
    }

    IEnumerator ReloadTimer()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadSpeed);
        currentAmmo = maxAmmo;
        ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
        reloading = false;
    }

    IEnumerator SwitchWeaponDelay()
    {
        yield return new WaitForSeconds(switchSpeed);

        // The current weapon will stop firing when switching
        CancelInvoke();

        if (weapon1Equipped)
        {
            SwitchWeapon(weapon2Data);
            weapon1Equipped = false;
        }
        else
        {
            SwitchWeapon(weapon1Data);
            weapon1Equipped = true;
        }

        switching = false;
        ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
    }
}
