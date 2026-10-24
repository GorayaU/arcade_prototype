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

    // Ammo variables
    [SerializeField] int clipSize = 12;
    int maxReserveAmmo;
    int reserveAmmo;
    int otherReserveAmmo;
    int currentAmmo;
    int previousAmmo;
    Transform cameraTransform;

    // Weapon switching variables
    [Header("Switching")]
    [SerializeField] Transform weaponLocation;
    WeaponStats weapon1Data;
    WeaponDefinition weapon1Definition;
    WeaponStats weapon2Data;
    WeaponDefinition weapon2Definition;
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
        weapon1Definition = GetComponent<CurrentWeapon>().GetPistolDefinition();
        weapon2Data = GetComponent<CurrentWeapon>().GetARifleData();
        weapon2Definition = GetComponent<CurrentWeapon>().GetRifleDefinition();
        // Sorting the ammo variables
        currentAmmo = weapon2Data.clipSize;
        previousAmmo = weapon1Data.clipSize;
        reserveAmmo = weapon2Data.clipSize * weapon2Definition.startingSpareMagazines;
        otherReserveAmmo = weapon1Data.clipSize * weapon1Definition.startingSpareMagazines;
        SwitchWeapon(weapon1Data, weapon1Definition, currentAmmo, reserveAmmo);
        weapon1Equipped = true;
        switching = false;

        // Updating UI information
        ammoText.text = $"Ammo: {currentAmmo}/{reserveAmmo}";
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
        if (reload.WasPerformedThisFrame() && currentAmmo != clipSize && reserveAmmo > 0)
        {
            StartCoroutine(ReloadTimer());
        }

        if (switchWeapon.WasPerformedThisFrame() && !switching)
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
            ammoText.text = $"Ammo: {currentAmmo}/{reserveAmmo}";

            StartCoroutine(CameraShakeTime());
        }
    }

    void SwitchWeapon(WeaponStats newStats, WeaponDefinition newDef, int ammo, int reserve)
    {
        weaponName = newStats.name;
        fireRate = newStats.fireRate;
        reloadSpeed = newStats.reloadSpeed;
        weaponDamage = newStats.damage;
        clipSize = newStats.clipSize;

        weaponNameText.text = weaponName;
        maxReserveAmmo = clipSize * newDef.maxSpareMagazines;
        currentAmmo = previousAmmo;
        reserveAmmo = otherReserveAmmo;

        // Records the ammo of the weapon held before switching
        previousAmmo = ammo;
        otherReserveAmmo = reserve;
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

        if (reserveAmmo >= clipSize)
        {
            reserveAmmo -= clipSize - currentAmmo;
            currentAmmo = clipSize;
        }
        else
        {
            int currentReserve = reserveAmmo;
            reserveAmmo -= clipSize - currentAmmo;
            currentAmmo += currentReserve;

            // Ensures the ammo won't go over the clip size
            if (currentAmmo > clipSize)
            {
                currentAmmo = clipSize;
            }

            // Ensures the reserve ammo isn't negative
            if (reserveAmmo < 0)
            {
                reserveAmmo = 0;
            }
        }
        ammoText.text = $"Ammo: {currentAmmo}/{reserveAmmo}";
        reloading = false;
    }

    IEnumerator SwitchWeaponDelay()
    {
        yield return new WaitForSeconds(switchSpeed);

        // The current weapon will stop firing when switching
        CancelInvoke();

        if (weapon1Equipped)
        {
            SwitchWeapon(weapon2Data, weapon2Definition, currentAmmo, reserveAmmo);
            weapon1Equipped = false;
        }
        else
        {
            SwitchWeapon(weapon1Data, weapon1Definition, currentAmmo, reserveAmmo);
            weapon1Equipped = true;
        }

        switching = false;
        ammoText.text = $"Ammo: {currentAmmo}/{reserveAmmo}";
    }

    void OnTriggerEnter(Collider other)
    {
        // Will trigger if the object has the "Pickup" layer
        if (other.gameObject.layer == 10)
        {
            reserveAmmo += clipSize;
            if (reserveAmmo > maxReserveAmmo)
            {
                reserveAmmo = maxReserveAmmo;
            }
            ammoText.text = $"Ammo: {currentAmmo}/{reserveAmmo}";
        }
    }
}
