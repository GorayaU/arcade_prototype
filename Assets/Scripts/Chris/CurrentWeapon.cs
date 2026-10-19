using UnityEngine;

public class CurrentWeapon : MonoBehaviour
{
    [SerializeField] WeaponStats pistolStats;
    [SerializeField] WeaponStats assaultRifleStats;
    [SerializeField] WeaponStats shotgunStats;

    public WeaponStats GetPistolData()
    {
        return pistolStats;
    }

    public WeaponStats GetARifleData()
    {
        return assaultRifleStats;
    }

    public WeaponStats GetShotgunData()
    {
        return shotgunStats;
    }
}
