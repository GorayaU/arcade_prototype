using System;
using System.Collections.Generic;
using UnityEngine;


/// The player's spare ammo, kept separately for every WeaponDefinition.

/// Internally this counts ROUNDS, because that makes reloading a half-empty magazine behave
/// the way players expect (no wasted rounds). The carry cap is still expressed in magazines:
/// maxSpareMagazines * magazineSize. Pickups hand over exactly one magazine's worth.

public class AmmoInventory : MonoBehaviour
{
    /// (weapon whose reserve changed)
    public event Action<WeaponDefinition> OnAmmoChanged;

    private readonly Dictionary<WeaponDefinition, int> reserveRounds = new Dictionary<WeaponDefinition, int>();

    /// Makes sure this weapon is tracked, seeding it with its starting ammo.
    public void Register(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.isMelee) return;
        if (reserveRounds.ContainsKey(weapon)) return;

        reserveRounds[weapon] = weapon.StartingSpareRounds;
        OnAmmoChanged?.Invoke(weapon);
    }

    public int GetReserveRounds(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.isMelee) return 0;
        return reserveRounds.TryGetValue(weapon, out int rounds) ? rounds : 0;
    }

    /// Whole magazines in reserve, for the HUD.
    public int GetSpareMagazines(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.isMelee || weapon.magazineSize <= 0) return 0;
        return GetReserveRounds(weapon) / weapon.magazineSize;
    }

    /// 0 = empty reserve, 1 = carrying the maximum 10 magazines.
    public float GetFill01(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.isMelee) return 1f;
        int max = weapon.MaxSpareRounds;
        return max <= 0 ? 1f : Mathf.Clamp01(GetReserveRounds(weapon) / (float)max);
    }

    /// At the carry cap, so no more of this ammo should drop.
    public bool IsFull(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.isMelee) return true;
        return GetReserveRounds(weapon) >= weapon.MaxSpareRounds;
    }

    public bool IsEmpty(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.isMelee) return false;
        return GetReserveRounds(weapon) <= 0;
    }
    
    /// Adds whole magazines, clamped to the carry cap.
    /// Returns false if the reserve was already full, so a pickup can refuse to be consumed.
    public bool AddMagazines(WeaponDefinition weapon, int magazines)
    {
        if (weapon == null || weapon.isMelee || magazines <= 0) return false;

        Register(weapon);
        if (IsFull(weapon)) return false;

        int before = reserveRounds[weapon];
        reserveRounds[weapon] = Mathf.Min(weapon.MaxSpareRounds, before + magazines * weapon.magazineSize);

        bool changed = reserveRounds[weapon] != before;
        if (changed) OnAmmoChanged?.Invoke(weapon);
        return changed;
    }

    public bool AddRounds(WeaponDefinition weapon, int rounds)
    {
        if (weapon == null || weapon.isMelee || rounds <= 0) return false;

        Register(weapon);
        int before = reserveRounds[weapon];
        reserveRounds[weapon] = Mathf.Min(weapon.MaxSpareRounds, before + rounds);

        bool changed = reserveRounds[weapon] != before;
        if (changed) OnAmmoChanged?.Invoke(weapon);
        return changed;
    }
    
    /// Pulls up to <paramref name="wanted"/> rounds out of the reserve for a reload.
    /// Returns how many were actually available.
    public int TakeRounds(WeaponDefinition weapon, int wanted)
    {
        if (weapon == null || weapon.isMelee || wanted <= 0) return 0;

        Register(weapon);
        int taken = Mathf.Min(wanted, reserveRounds[weapon]);
        if (taken <= 0) return 0;

        reserveRounds[weapon] -= taken;
        OnAmmoChanged?.Invoke(weapon);
        return taken;
    }

    /// Debug / cheat helper.
    public void FillAll(IEnumerable<WeaponDefinition> weapons)
    {
        foreach (WeaponDefinition weapon in weapons)
        {
            if (weapon == null || weapon.isMelee) continue;
            Register(weapon);
            reserveRounds[weapon] = weapon.MaxSpareRounds;
            OnAmmoChanged?.Invoke(weapon);
        }
    }
}
