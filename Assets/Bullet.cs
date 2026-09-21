using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float bulletLifetime = 2f;

    void Awake()
    {
        StartCoroutine(DestroyBulletAfterSpawn());
    }

    void OnTriggerEnter(Collider other)
    {
        // Ensures the bullet destroys anything it touches other than the player and other projectiles
        if (!other.gameObject.CompareTag("Player") && !other.gameObject.CompareTag("Bullet"))
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
