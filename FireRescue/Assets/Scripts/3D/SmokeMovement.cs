using UnityEngine;
using System.Collections;

public class SmokeMovement : MonoBehaviour
{
    public float scaleSpeed = 0.1f; // Velocidad de oscilación de escala
    public float distortionSpeed = 1f; // Velocidad de distorsión
    public float maxScale = 1.2f; // Escala máxima
    public float minScale = 0.8f; // Escala mínima

    private Vector3 initialScale; // Escala inicial
    private Quaternion initialRotation; // Rotación inicial

    public float duracionScale = 2f;

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

    public IEnumerator PutOutSmoke()
    {
        yield return StartCoroutine(ScaleDownAndDestroy());
    }

    private IEnumerator ScaleDownAndDestroy()
    {
        Vector3 escalaInicial = transform.localScale;
        Vector3 escalaFinal = Vector3.zero;
        float tiempo = 0f;

        while (tiempo < duracionScale)
        {
            transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, tiempo / duracionScale);
            tiempo += Time.deltaTime;
            yield return null;
        }

        transform.localScale = escalaFinal;
        Destroy(gameObject); // Destruye el objeto después de la reducción de escala
    }
}
