using System.Collections;
using Udey.Core;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float bulletLifetime = 2f;
    [SerializeField] float bulletDamage = 15;

    void Awake()
    {
        // Retrieve damage and range stats from the current weapon
        bulletDamage = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerShooting>().GetWeaponDamage();
        bulletLifetime = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerShooting>().GetBulletRange();

        StartCoroutine(DestroyBulletAfterSpawn());
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
