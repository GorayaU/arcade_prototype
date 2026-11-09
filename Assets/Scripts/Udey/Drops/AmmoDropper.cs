using System.Collections.Generic;
using UnityEngine;
using Udey.Core;


/// Rolls for an ammo drop when this enemy dies. Put it on every enemy prefab.
/// All the tuning lives on the shared AmmoDropSettings asset; the only per-enemy knob
/// is the multiplier below, so a Tank can be made more generous than a Skalith.

[RequireComponent(typeof(Health))]
public class AmmoDropper : MonoBehaviour
{
    [SerializeField] private AmmoDropSettings settings;

    [Tooltip("Multiplies this enemy's drop chance. Give tougher enemies a higher number.")]
    [SerializeField] private float enemyChanceMultiplier = 1f;

    [Tooltip("Log the chance and the chosen weapon to the Console. Useful while balancing.")]
    [SerializeField] private bool logRolls = false;

    private Health health;

    /// Scratch list, reused so a kill doesn't allocate.
    private readonly List<Candidate> candidates = new List<Candidate>();

    private struct Candidate
    {
        public WeaponDefinition Weapon;
        public float Weight;
        public float Scarcity;
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    private void HandleDeath(DamageInfo killingBlow)
    {
        if (settings == null || settings.pickupPrefab == null) return;

        PlayerRig player = PlayerRig.Instance;
        if (player == null || player.Ammo == null || player.Weapons == null) return;

        BuildCandidates(player);
        if (candidates.Count == 0) return; // everything is capped — nothing should drop

        float chance = CalculateChance(player, killingBlow);

        bool dropped = Random.value <= chance;
        if (logRolls)
        {
            Debug.Log($"[AmmoDropper] {name} died ({killingBlow.Type}) — chance {chance:P0} → " +
                      (dropped ? "DROP" : "no drop"));
        }

        if (!dropped) return;

        WeaponDefinition chosen = PickWeightedWeapon();
        if (chosen != null) SpawnPickup(chosen);
    }

    /// Every gun the player carries that isn't already at the magazine cap.
    private void BuildCandidates(PlayerRig player)
    {
        candidates.Clear();

        IReadOnlyList<WeaponDefinition> loadout = player.Weapons.Loadout;

        for (int i = 0; i < loadout.Count; i++)
        {
            WeaponDefinition weapon = loadout[i];
            if (weapon == null || weapon.isMelee) continue;
            if (player.Ammo.IsFull(weapon)) continue; // "if they have the maximum, it won't drop"

            float fill = player.Ammo.GetFill01(weapon);
            float weight = Mathf.Max(0f, weapon.dropWeight * weapon.scarcityWeightCurve.Evaluate(fill));
            if (weight <= 0f) continue;

            candidates.Add(new Candidate
            {
                Weapon = weapon,
                Weight = weight,
                Scarcity = 1f - fill
            });
        }
    }

    private float CalculateChance(PlayerRig player, DamageInfo killingBlow)
    {
        // Drive the overall chance off whichever weapon the player most needs.
        float worstScarcity = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Scarcity > worstScarcity) worstScarcity = candidates[i].Scarcity;
        }

        float chance = settings.baseDropChance
                       * settings.scarcityChanceCurve.Evaluate(worstScarcity)
                       * Mathf.Max(0f, enemyChanceMultiplier);

        if (killingBlow.Type == DamageType.Melee) chance *= settings.meleeKillMultiplier;

        chance = Mathf.Clamp(chance, 0f, settings.maxDropChance);

        if (settings.guaranteeDropWhenCompletelyDry && player.Weapons.IsCompletelyOutOfAmmo())
        {
            chance = 1f;
        }

        return chance;
    }

    private WeaponDefinition PickWeightedWeapon()
    {
        float total = 0f;
        for (int i = 0; i < candidates.Count; i++) total += candidates[i].Weight;
        if (total <= 0f) return null;

        float roll = Random.value * total;

        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= candidates[i].Weight;
            if (roll <= 0f) return candidates[i].Weapon;
        }

        return candidates[candidates.Count - 1].Weapon;
    }

    private void SpawnPickup(WeaponDefinition weapon)
    {
        Vector2 scatter = Random.insideUnitCircle * settings.spawnScatterRadius;
        Vector3 position = transform.position
                           + Vector3.up * settings.spawnHeightOffset
                           + new Vector3(scatter.x, 0f, scatter.y);

        GameObject instance = Instantiate(settings.pickupPrefab, position, Quaternion.identity);

        AmmoPickup pickup = instance.GetComponent<AmmoPickup>();
        if (pickup != null)
        {
            pickup.Configure(weapon, settings.magazinesPerPickup);
        }
        else
        {
            Debug.LogWarning("[AmmoDropper] The pickup prefab has no AmmoPickup component.", settings.pickupPrefab);
        }
    }
}
