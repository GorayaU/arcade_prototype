using UnityEngine;

public class Bullet : MonoBehaviour
{

    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            Destroy(other.gameObject);
        }
    }
}
