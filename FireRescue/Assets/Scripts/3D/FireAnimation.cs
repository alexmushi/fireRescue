using UnityEngine;
using System.Collections;

public class FireAnimation : MonoBehaviour
{
    public float scaleSpeed = 2f; // Velocidad de oscilación de escala
    public float intensity = 0.1f; // Intensidad de las ondulaciones
    public float flickerSpeed = 10f; // Velocidad del parpadeo

    private Vector3 initialScale;
    private Vector3 initialPosition;

    public float duracionScale = 2f;

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

    public IEnumerator PutOutFire()
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