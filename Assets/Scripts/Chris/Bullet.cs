using System.Collections;
using Udey.Core;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float bulletLifetime = 2f;
    float bulletDamage;

    void Awake()
    {
        StartCoroutine(DestroyBulletAfterSpawn());
        // Retrieve damage stat from the current weapon
        bulletDamage = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerShooting>().GetWeaponDamage();
    }

    void OnTriggerEnter(Collider other)
    {
        // Any object with the tag "Enemy" will take damage depending on the weapon
        if (other.gameObject.CompareTag("Enemy"))
        {
            IDamageable damageable = other.gameObject.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead)
            {
                return;
            }
            damageable.TakeDamage(new DamageInfo(bulletDamage, DamageType.Bullet, Vector3.forward, Vector3.forward, gameObject));

            StopCoroutine(DestroyBulletAfterSpawn());
            Destroy(gameObject);
        }
    }

    IEnumerator DestroyBulletAfterSpawn()
    {
        yield return new WaitForSeconds(bulletLifetime);
        Destroy(gameObject);
    }
}
