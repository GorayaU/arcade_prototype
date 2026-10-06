using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "ScriptableObjects/WeaponStats", order = 1)]
public class WeaponStats : ScriptableObject
{
    [field: SerializeField] public float fireRate;
    [field: SerializeField] public float reloadSpeed;
    [field: SerializeField] public float damage;
    [field: SerializeField] public int maxAmmo;
}
