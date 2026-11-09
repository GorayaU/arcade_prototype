using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Shows the current weapon, the rounds in the magazine, and the spare reserve.
/// Reserve is shown both ways — "72 (6 mags)" — because the carry cap is defined in
/// magazines but what you actually spend is rounds.

/// Every text/image field is optional; wire up only what your HUD has.

public class AmmoHUD : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Leave empty to use the player's WeaponController.")]
    [SerializeField] private WeaponController weapons;

    [Header("Text")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text magazineText;
    [SerializeField] private TMP_Text reserveText;

    [Header("Images")]
    [SerializeField] private Image weaponIcon;

    [Tooltip("Optional Image set to Filled — shows reload progress.")]
    [SerializeField] private Image reloadFill;

    [SerializeField] private GameObject reloadIndicatorRoot;

    [Header("Formatting")]
    [SerializeField] private string meleeLabel = "—";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowAmmoColor = new Color(0.95f, 0.45f, 0.3f);

    [Tooltip("Magazine is 'low' at or below this fraction of its size.")]
    [Range(0f, 1f)][SerializeField] private float lowMagazineThreshold = 0.3f;

    private AmmoInventory ammo;

    private void Start()
    {
        if (weapons == null && PlayerRig.Instance != null) weapons = PlayerRig.Instance.Weapons;
        if (weapons == null) weapons = FindFirstObjectByType<WeaponController>();

        if (weapons == null)
        {
            Debug.LogError("[AmmoHUD] No WeaponController assigned or found in the scene.", this);
            enabled = false;
            return;
        }

        ammo = weapons.GetComponent<AmmoInventory>();

        weapons.OnWeaponChanged += HandleWeaponChanged;
        weapons.OnAmmoStateChanged += Refresh;
        if (ammo != null) ammo.OnAmmoChanged += HandleAmmoChanged;

        HandleWeaponChanged(weapons.CurrentWeapon);
    }

    private void OnDestroy()
    {
        if (weapons != null)
        {
            weapons.OnWeaponChanged -= HandleWeaponChanged;
            weapons.OnAmmoStateChanged -= Refresh;
        }

        if (ammo != null) ammo.OnAmmoChanged -= HandleAmmoChanged;
    }

    private void Update()
    {
        // Reload progress is the one thing that has to update every frame.
        bool reloading = weapons.IsReloading;

        if (reloadIndicatorRoot != null && reloadIndicatorRoot.activeSelf != reloading)
        {
            reloadIndicatorRoot.SetActive(reloading);
        }

        if (reloadFill != null)
        {
            reloadFill.fillAmount = reloading ? weapons.ReloadProgress01 : 0f;
        }
    }

    private void HandleAmmoChanged(WeaponDefinition changed) => Refresh();

    private void HandleWeaponChanged(WeaponDefinition weapon)
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = weapon != null ? weapon.displayName : string.Empty;
        }

        if (weaponIcon != null)
        {
            weaponIcon.sprite = weapon != null ? weapon.icon : null;
            weaponIcon.enabled = weaponIcon.sprite != null;
        }

        Refresh();
    }

    private void Refresh()
    {
        WeaponDefinition weapon = weapons.CurrentWeapon;

        if (weapon == null)
        {
            SetText(magazineText, string.Empty);
            SetText(reserveText, string.Empty);
            return;
        }

        if (weapon.isMelee)
        {
            SetText(magazineText, meleeLabel);
            SetText(reserveText, string.Empty);
            if (magazineText != null) magazineText.color = normalColor;
            return;
        }

        int inMagazine = weapons.RoundsInMagazine;
        SetText(magazineText, $"{inMagazine} / {weapon.magazineSize}");

        if (magazineText != null)
        {
            bool low = inMagazine <= weapon.magazineSize * lowMagazineThreshold;
            magazineText.color = low ? lowAmmoColor : normalColor;
        }

        if (reserveText != null && ammo != null)
        {
            int rounds = ammo.GetReserveRounds(weapon);
            int mags = ammo.GetSpareMagazines(weapon);
            string cap = ammo.IsFull(weapon) ? " MAX" : string.Empty;
            reserveText.text = $"{rounds} ({mags}/{weapon.maxSpareMagazines} mags){cap}";
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }
}
