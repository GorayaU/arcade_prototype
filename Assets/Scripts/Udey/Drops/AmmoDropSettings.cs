using UnityEngine;


/// Shared tuning for ammo drops, so every enemy in the game pulls from one asset.
/// Create via Assets > Create > Game > Ammo Drop Settings.

/// How a drop is decided, in order:
///   1. Build the list of weapons that are eligible — not melee, not already at the
///      10-magazine cap.
///   2. Work out the scarcity of the emptiest eligible weapon (0 = full, 1 = bone dry).
///   3. chance = baseDropChance * scarcityChanceCurve(scarcity) * enemy multiplier,
///      then * meleeKillMultiplier if the killing blow was melee. Clamped to maxDropChance.
///   4. If the roll succeeds, pick WHICH weapon by weighted random, using each weapon's
///      dropWeight * its own scarcityWeightCurve.

[CreateAssetMenu(fileName = "AmmoDropSettings", menuName = "Game/Ammo Drop Settings")]
public class AmmoDropSettings : ScriptableObject
{
    [Header("Pickup")]
    [Tooltip("Prefab with an AmmoPickup component. The dropper tells it which weapon it's for.")]
    public GameObject pickupPrefab;

    [Tooltip("How many magazines one pickup is worth. The brief says 1.")]
    public int magazinesPerPickup = 1;

    [Tooltip("Spawned this far above the enemy's position, so it doesn't clip into the floor.")]
    public float spawnHeightOffset = 0.6f;

    [Tooltip("Random horizontal scatter so stacked kills don't pile pickups on one spot.")]
    public float spawnScatterRadius = 0.4f;

    [Header("Base chance")]
    [Range(0f, 1f)] public float baseDropChance = 0.22f;

    [Tooltip("Hard ceiling, so drops never become guaranteed by accident.")]
    [Range(0f, 1f)] public float maxDropChance = 0.85f;

    [Header("Scarcity")]
    [Tooltip("x = scarcity of the player's emptiest eligible weapon (0 = full, 1 = empty). " +
             "y = multiplier on the base chance. This is what makes drops ramp up when you're dry.")]
    public AnimationCurve scarcityChanceCurve = new AnimationCurve(
        new Keyframe(0f, 0.6f),
        new Keyframe(0.5f, 1.3f),
        new Keyframe(1f, 2.8f));

    [Header("Melee kills")]
    [Tooltip("Chance multiplier when the killing blow was melee. Rewards the risk.")]
    public float meleeKillMultiplier = 1.8f;

    [Header("Safety net")]
    [Tooltip("Force a drop when the player has nothing loaded and nothing in reserve for any gun. " +
             "Stops a run from becoming unwinnable.")]
    public bool guaranteeDropWhenCompletelyDry = true;

    private void OnValidate()
    {
        magazinesPerPickup = Mathf.Max(1, magazinesPerPickup);
        meleeKillMultiplier = Mathf.Max(1f, meleeKillMultiplier);
        if (maxDropChance < baseDropChance) maxDropChance = baseDropChance;
    }
}
