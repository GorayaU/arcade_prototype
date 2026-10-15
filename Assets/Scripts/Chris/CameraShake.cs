using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] float shakeAmount = 0.02f;

    void Awake()
    {
        transform.position = cameraTransform.position;
    }

    void Update()
    {
        // Moves the camera a random direction based on its current position
        transform.position = cameraTransform.position + Random.insideUnitSphere * shakeAmount;
    }
}
