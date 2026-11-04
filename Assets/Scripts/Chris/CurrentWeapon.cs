using UnityEngine;

public class CurrentWeapon : MonoBehaviour
{
    [SerializeField] WeaponStats pistolStats;
    [SerializeField] WeaponDefinition pistolDefinition;
    [SerializeField] WeaponStats assaultRifleStats;
    [SerializeField] WeaponDefinition rifleDefinition;
    [SerializeField] WeaponStats shotgunStats;
    [SerializeField] WeaponDefinition shotgunDefinition;

    public WeaponStats GetPistolData()
    {
        return pistolStats;
    }

    public WeaponDefinition GetPistolDefinition()
    {
        return pistolDefinition;
    }

    public WeaponStats GetARifleData()
    {
        return assaultRifleStats;
    }

    public WeaponDefinition GetRifleDefinition()
    {
        return rifleDefinition;
    }

    public WeaponStats GetShotgunData()
    {
        return shotgunStats;
    }

    public WeaponDefinition GetShotgunDefinition()
    {
        return shotgunDefinition;
    }
}
