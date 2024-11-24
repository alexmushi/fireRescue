using UnityEngine;

public class MovePOI : MonoBehaviour
{
    // The height of the hover
    public float hoverHeight = 0.5f;

    // The speed of the hover motion
    public float hoverSpeed = 2f;

    private Vector3 initialPosition;

    void Start()
    {
        // Store the initial position at the start
        initialPosition = transform.position;
    }

    void Update()
    {
        // Calculate the new Y position using a sine wave
        float newY = initialPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        
        // Update the object's position
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }
}
