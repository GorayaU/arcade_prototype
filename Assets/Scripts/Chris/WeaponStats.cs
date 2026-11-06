using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "ScriptableObjects/WeaponStats", order = 1)]
public class WeaponStats : ScriptableObject
{
    public int index;
    public new string name;
    public float fireRate;
    public float reloadSpeed;
    public float damage;
    public int clipSize;
    [Tooltip("Only applies to the shotgun")]
    public float spread;
    public float range;
}
