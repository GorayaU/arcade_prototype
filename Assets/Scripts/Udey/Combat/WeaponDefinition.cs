using UnityEngine;


/// All the tuning for one weapon, stored as an asset so designers can balance without
/// touching prefabs. Create via Assets > Create > Game > Weapon Definition.

/// Ammo is tracked per-definition, so every weapon has its own independent reserve.
 
[CreateAssetMenu(fileName = "WPN_NewWeapon", menuName = "Game/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Pistol";
    public Sprite icon;

    [Tooltip("Tints the ammo pickup so the player can tell at a glance what dropped.")]
    public Color pickupColor = new Color(1f, 0.82f, 0.25f);

    [Header("Type")]
    [Tooltip("Melee weapons use no ammo, never reload, and never generate ammo drops.")]
    public bool isMelee = false;

    [Header("Damage")]
    public float damage = 12f;

    [Tooltip("Hitscan range in metres. For melee this is the reach of the swing.")]
    public float range = 60f;

    [Tooltip("Shots per second.")]
    public float fireRate = 4f;

    [Tooltip("Hold to keep firing, instead of one shot per click.")]
    public bool automatic = false;

    [Header("Melee only")]
    [Tooltip("Half-angle of the swing is this / 2. 360 hits everything around you.")]
    [Range(10f, 360f)] public float meleeArcDegrees = 110f;

    [Header("Ammo (ignored for melee)")]
    public int magazineSize = 12;

    [Tooltip("The carry cap, in magazines. The brief says 10 for every weapon.")]
    public int maxSpareMagazines = 10;

    [Tooltip("How many spare magazines the player starts the run with.")]
    public int startingSpareMagazines = 4;

    public float reloadTime = 1.4f;

    [Header("Ammo drops")]
    [Tooltip("Relative chance this weapon's ammo is the one that drops. Weak, plentiful " +
             "weapons (the pistol) want a high number; strong ones want a low number.")]
    public float dropWeight = 3f;

    [Tooltip("x = how full the player's reserve is (0 = bone dry, 1 = capped). " +
             "y = multiplier on this weapon's drop weight. Curving up on the left is what " +
             "makes empty weapons start dropping more.")]
    public AnimationCurve scarcityWeightCurve = new AnimationCurve(
        new Keyframe(0f, 5f),
        new Keyframe(0.35f, 2f),
        new Keyframe(1f, 1f));

    /// Reserve cap expressed in rounds rather than magazines.
    public int MaxSpareRounds => Mathf.Max(0, magazineSize * maxSpareMagazines);

    public int StartingSpareRounds => Mathf.Clamp(
        magazineSize * startingSpareMagazines, 0, MaxSpareRounds);

    public float SecondsBetweenShots => fireRate <= 0f ? 0f : 1f / fireRate;

    private void OnValidate()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        maxSpareMagazines = Mathf.Max(0, maxSpareMagazines);
        startingSpareMagazines = Mathf.Clamp(startingSpareMagazines, 0, maxSpareMagazines);
        dropWeight = Mathf.Max(0f, dropWeight);
        fireRate = Mathf.Max(0.01f, fireRate);
    }
}
