using System.Collections;
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
        // Ensures the bullet destroys any object with the tag "Enemy"
        if (other.gameObject.CompareTag("Enemy"))
        {
            Destroy(other.gameObject);
        }
    }

    IEnumerator DestroyBulletAfterSpawn()
    {
        yield return new WaitForSeconds(bulletLifetime);
        Destroy(gameObject);
    }
}
