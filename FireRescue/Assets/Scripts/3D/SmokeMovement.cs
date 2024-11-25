using UnityEngine;

public class SmokeMovement : MonoBehaviour
{
    public float scaleSpeed = 0.1f; // Velocidad de oscilación de escala
    public float distortionSpeed = 1f; // Velocidad de distorsión
    public float maxScale = 1.2f; // Escala máxima
    public float minScale = 0.8f; // Escala mínima

    private Vector3 initialScale; // Escala inicial
    private Quaternion initialRotation; // Rotación inicial

    void Start()
    {
        initialScale = transform.localScale;
        initialRotation = transform.rotation; // Guardar la orientación inicial
    }

    void Update()
    {
        // Oscilar escala
        float scaleOffset = Mathf.Sin(Time.time * scaleSpeed) * 0.1f;
        transform.localScale = initialScale * (1 + scaleOffset);

        // Simular distorsión sin alterar la rotación inicial
        float distortionX = Mathf.Sin(Time.time * distortionSpeed) * 0.02f;
        float distortionY = Mathf.Cos(Time.time * distortionSpeed) * 0.02f;

        // Aplicar ligeros desplazamientos sin cambiar la orientación
        transform.rotation = initialRotation * Quaternion.Euler(distortionX, distortionY, 0);
    }
}
