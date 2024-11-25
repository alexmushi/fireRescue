using UnityEngine;
using System.Collections;

public class Shakes : MonoBehaviour
{
    public static Shakes Instance { get; private set; }

    public float shakeDuration = 0.5f;  // Duration of the shake
    public float shakeMagnitude = 0.2f; // Magnitude of the shake

    private Vector3 originalPosition;

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Save the original camera position
        originalPosition = transform.localPosition;
    }

    public void TriggerShake()
    {
        // Start the shake coroutine
        StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Generate a random offset within the shake magnitude
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude;

            // Apply the offset to the camera position
            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;

            // Wait until the next frame
            yield return null;
        }

        // Reset the camera to its original position after the shake
        transform.localPosition = originalPosition;
    }

    public void TriggerWallShake(Transform target, float duration, float magnitude)
    {
        StartCoroutine(WallShake(target, duration, magnitude));
    }

    private IEnumerator WallShake(Transform target, float duration, float magnitude)
    {
        Vector3 originalPosition = target.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float offsetX = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float offsetY = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float offsetZ = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            target.localPosition = originalPosition + new Vector3(offsetX, offsetY, offsetZ);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPosition;
    }
}