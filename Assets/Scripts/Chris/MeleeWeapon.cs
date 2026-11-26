using Udey.Core;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    float meleeDamage = 12.0f;

    void Awake()
    {
        // Retrieve damage stat from the player melee
        meleeDamage = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMelee>().GetMeleeDamage();
    }

    void OnTriggerEnter(Collider other)
    {
        // Any object with the tag "Enemy" will be damaged if the weapon collides with it
        if (other.gameObject.CompareTag("Enemy"))
        {
            IDamageable damageable = other.gameObject.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead)
            {
                return;
            }
            damageable.TakeDamage(new DamageInfo(meleeDamage, DamageType.Melee, Vector3.forward, Vector3.forward, gameObject));
        }
    }
}
