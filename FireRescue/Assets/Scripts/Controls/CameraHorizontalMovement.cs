using UnityEngine;

public class CameraHorizontalMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Velocidad de movimiento

    void Start()
    {
        // Posición inicial
        transform.position = new Vector3(4.55f, 4.72f, -2.32f);

        // Rotación inicial
        transform.rotation = Quaternion.Euler(46.659f, 0f, 0f);
    }

    void Update()
    {
        // Obtener entrada del jugador
        float horizontal = Input.GetAxis("Horizontal"); // Movimiento a los lados (A/D o flechas)
        float vertical = Input.GetAxis("Vertical"); // Movimiento hacia adelante/atrás (W/S o flechas)

        // Crear vector de movimiento en el plano horizontal
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;

        // Mover la cámara
        transform.Translate(movement, Space.World);

        // Limitar posición de la cámara
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -2f, 12f); // Limitar X
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, -5f, 5f); // Limitar Z
        transform.position = clampedPosition;
    }
}
