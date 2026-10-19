using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Udey.Core;

/// Holds the player's loadout, fires it, reloads it and switches between weapons.
/// Guns are hitscan: a raycast straight out of the camera. Melee is just another slot in the
/// same list, flagged isMelee, that costs no ammo.

/// Uses the stock InputSystem_Actions asset: Attack to fire, Previous / Next to switch.
/// The default asset has no Reload action, so reload defaults to the Interact action —
/// change Reload Action Name below, or add a proper Reload action to the asset.

[RequireComponent(typeof(PlayerInput))]
public class WeaponController : MonoBehaviour
{
    [Header("Loadout")]
    [Tooltip("Order matters — this is the order Previous / Next and the number keys cycle through.")]
    [SerializeField] private List<WeaponDefinition> loadout = new List<WeaponDefinition>();

    [SerializeField] private int startingIndex = 0;

    [Header("References")]
    [Tooltip("Where shots originate. Use the player camera so you hit what the crosshair covers.")]
    [SerializeField] private Transform aimOrigin;

    [SerializeField] private AmmoInventory ammo;

    [Header("Hit detection")]
    [Tooltip("What bullets and swings can hit. Exclude the Player layer.")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Input")]
    [Tooltip("Action used to reload. The stock asset has no Reload action, so this defaults " +
             "to Interact. Add a Reload action to InputSystem_Actions and rename this if you prefer.")]
    [SerializeField] private string reloadActionName = "Interact";

    [SerializeField] private bool autoReloadWhenEmpty = true;
    [SerializeField] private bool switchWithScrollWheel = true;
    [SerializeField] private bool switchWithNumberKeys = true;

    // --- Public state, read by the HUD and the ammo drop system ---

    public IReadOnlyList<WeaponDefinition> Loadout => loadout;
    public WeaponDefinition CurrentWeapon =>
        IsValidIndex(currentIndex) ? loadout[currentIndex] : null;
    public int CurrentIndex => currentIndex;
    public int RoundsInMagazine => IsValidIndex(currentIndex) ? magazines[currentIndex] : 0;
    public bool IsReloading { get; private set; }
    public float ReloadProgress01 { get; private set; }

    public event Action<WeaponDefinition> OnWeaponChanged;
    public event Action OnAmmoStateChanged;
    public event Action<WeaponDefinition> OnFired;
    public event Action<WeaponDefinition> OnReloadStarted;

    // --- Internals ---

    private int currentIndex;
    private int[] magazines;
    private float nextFireTime;
    private float reloadStartTime;
    private float reloadEndTime;

    private InputAction attackAction;
    private InputAction nextAction;
    private InputAction previousAction;
    private InputAction reloadAction;

    private readonly List<IDamageable> meleeHitsThisSwing = new List<IDamageable>();
    private readonly Collider[] meleeOverlapBuffer = new Collider[32];

    private void Awake()
    {
        if (ammo == null) ammo = GetComponent<AmmoInventory>();
        if (aimOrigin == null && Camera.main != null) aimOrigin = Camera.main.transform;

        if (ammo == null || aimOrigin == null)
        {
            Debug.LogError("[WeaponController] Needs an AmmoInventory and an Aim Origin (the camera).", this);
            enabled = false;
            return;
        }

        magazines = new int[loadout.Count];

        for (int i = 0; i < loadout.Count; i++)
        {
            WeaponDefinition weapon = loadout[i];
            if (weapon == null) continue;

            ammo.Register(weapon);
            // Every gun starts with a full magazine loaded, on top of its spare magazines.
            magazines[i] = weapon.isMelee ? 0 : weapon.magazineSize;
        }

        PlayerInput input = GetComponent<PlayerInput>();
        attackAction = input.actions.FindAction("Attack", throwIfNotFound: false);
        nextAction = input.actions.FindAction("Next", throwIfNotFound: false);
        previousAction = input.actions.FindAction("Previous", throwIfNotFound: false);
        reloadAction = input.actions.FindAction(reloadActionName, throwIfNotFound: false);

        if (attackAction == null)
        {
            Debug.LogError("[WeaponController] No 'Attack' action found on the Input Actions asset.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        currentIndex = -1;
        SelectWeapon(Mathf.Clamp(startingIndex, 0, Mathf.Max(0, loadout.Count - 1)));
    }

    private void Update()
    {
        HandleSwitching();
        HandleReloading();
        HandleFiring();
    }

    // ---------------------------------------------------------------- switching

    private void HandleSwitching()
    {
        if (loadout.Count <= 1) return;

        if (nextAction != null && nextAction.WasPressedThisFrame()) CycleWeapon(1);
        if (previousAction != null && previousAction.WasPressedThisFrame()) CycleWeapon(-1);

        if (switchWithScrollWheel && Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0.01f) CycleWeapon(1);
            else if (scroll < -0.01f) CycleWeapon(-1);
        }

        if (switchWithNumberKeys && Keyboard.current != null)
        {
            int slots = Mathf.Min(loadout.Count, 9);
            for (int i = 0; i < slots; i++)
            {
                if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
                {
                    SelectWeapon(i);
                    break;
                }
            }
        }
    }

    public void CycleWeapon(int direction)
    {
        if (loadout.Count == 0) return;
        int index = (currentIndex + direction + loadout.Count) % loadout.Count;
        SelectWeapon(index);
    }

    public void SelectWeapon(int index)
    {
        if (!IsValidIndex(index) || index == currentIndex) return;

        CancelReload();
        currentIndex = index;
        nextFireTime = Time.time; // no artificial delay after a swap

        OnWeaponChanged?.Invoke(CurrentWeapon);
        OnAmmoStateChanged?.Invoke();
    }

    // ---------------------------------------------------------------- reloading

    private void HandleReloading()
    {
        if (IsReloading)
        {
            float duration = Mathf.Max(0.01f, reloadEndTime - reloadStartTime);
            ReloadProgress01 = Mathf.Clamp01((Time.time - reloadStartTime) / duration);

            if (Time.time >= reloadEndTime) FinishReload();
            return;
        }

        if (reloadAction != null && reloadAction.WasPressedThisFrame()) StartReload();
    }

    public void StartReload()
    {
        WeaponDefinition weapon = CurrentWeapon;
        if (weapon == null || weapon.isMelee || IsReloading) return;
        if (magazines[currentIndex] >= weapon.magazineSize) return;
        if (ammo.GetReserveRounds(weapon) <= 0) return;

        IsReloading = true;
        ReloadProgress01 = 0f;
        reloadStartTime = Time.time;
        reloadEndTime = Time.time + weapon.reloadTime;

        OnReloadStarted?.Invoke(weapon);
    }

    private void FinishReload()
    {
        WeaponDefinition weapon = CurrentWeapon;
        IsReloading = false;
        ReloadProgress01 = 0f;
        if (weapon == null || weapon.isMelee) return;

        // Pull only what's missing, so a partial magazine isn't thrown away.
        int needed = weapon.magazineSize - magazines[currentIndex];
        magazines[currentIndex] += ammo.TakeRounds(weapon, needed);

        OnAmmoStateChanged?.Invoke();
    }

    private void CancelReload()
    {
        IsReloading = false;
        ReloadProgress01 = 0f;
    }

    // ---------------------------------------------------------------- firing

    private void HandleFiring()
    {
        WeaponDefinition weapon = CurrentWeapon;
        if (weapon == null || IsReloading) return;

        bool wantsToFire = weapon.automatic
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame();

        if (!wantsToFire || Time.time < nextFireTime) return;

        if (weapon.isMelee)
        {
            nextFireTime = Time.time + weapon.SecondsBetweenShots;
            MeleeSwing(weapon);
            OnFired?.Invoke(weapon);
            return;
        }

        if (magazines[currentIndex] <= 0)
        {
            if (autoReloadWhenEmpty) StartReload();
            return;
        }

        nextFireTime = Time.time + weapon.SecondsBetweenShots;
        magazines[currentIndex]--;
        FireHitscan(weapon);

        OnFired?.Invoke(weapon);
        OnAmmoStateChanged?.Invoke();

        if (magazines[currentIndex] <= 0 && autoReloadWhenEmpty) StartReload();
    }

    private void FireHitscan(WeaponDefinition weapon)
    {
        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, weapon.range, hitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // GetComponentInParent so hitboxes can live on child colliders.
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead) return;

        damageable.TakeDamage(new DamageInfo(
            weapon.damage, DamageType.Bullet, hit.point, ray.direction, gameObject));
    }

    private void MeleeSwing(WeaponDefinition weapon)
    {
        meleeHitsThisSwing.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            aimOrigin.position, weapon.range, meleeOverlapBuffer, hitMask, QueryTriggerInteraction.Ignore);

        float halfArc = weapon.meleeArcDegrees * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Collider collider = meleeOverlapBuffer[i];
            if (collider == null) continue;

            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;

            // Don't hit yourself, and don't hit the same enemy twice with one swing.
            if (damageable is Component component && component.transform.root == transform.root) continue;
            if (meleeHitsThisSwing.Contains(damageable)) continue;

            Vector3 toTarget = collider.bounds.center - aimOrigin.position;
            if (weapon.meleeArcDegrees < 360f && Vector3.Angle(aimOrigin.forward, toTarget) > halfArc) continue;

            meleeHitsThisSwing.Add(damageable);

            Vector3 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : aimOrigin.forward;
            damageable.TakeDamage(new DamageInfo(
                weapon.damage, DamageType.Melee, collider.bounds.center, direction, gameObject));
        }
    }

    // ---------------------------------------------------------------- queries
    
    /// True when every gun in the loadout has an empty magazine AND an empty reserve.
    /// The drop system uses this to guarantee a pickup so the player can't get softlocked.

    public bool IsCompletelyOutOfAmmo()
    {
        bool hasAnyGun = false;

        for (int i = 0; i < loadout.Count; i++)
        {
            WeaponDefinition weapon = loadout[i];
            if (weapon == null || weapon.isMelee) continue;

            hasAnyGun = true;
            if (magazines[i] > 0) return false;
            if (ammo.GetReserveRounds(weapon) > 0) return false;
        }

        return hasAnyGun;
    }

    private bool IsValidIndex(int index) => index >= 0 && index < loadout.Count && loadout[index] != null;
}
