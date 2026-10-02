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
