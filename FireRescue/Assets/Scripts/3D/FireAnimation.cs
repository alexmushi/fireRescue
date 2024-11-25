using UnityEngine;

public class FireAnimation : MonoBehaviour
{
    public float scaleSpeed = 2f; // Velocidad de oscilación de escala
    public float intensity = 0.1f; // Intensidad de las ondulaciones
    public float flickerSpeed = 10f; // Velocidad del parpadeo

    private Vector3 initialScale;
    private Vector3 initialPosition;

    void Start()
    {
        initialScale = transform.localScale;
        initialPosition = transform.position;
    }

    void Update()
    {
        // Oscilar la escala para simular la intensidad del fuego
        float scaleOffset = Mathf.Sin(Time.time * scaleSpeed) * intensity;
        transform.localScale = initialScale + new Vector3(scaleOffset, scaleOffset * 2, scaleOffset);

        // Simular parpadeo con pequeñas variaciones en la posición
        float flickerX = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * intensity - (intensity / 2);
        float flickerZ = Mathf.PerlinNoise(0f, Time.time * flickerSpeed) * intensity - (intensity / 2);
        transform.position = initialPosition + new Vector3(flickerX, 0, flickerZ);
    }
}