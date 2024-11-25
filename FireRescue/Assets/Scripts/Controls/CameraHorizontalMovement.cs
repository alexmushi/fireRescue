using UnityEngine;

public class CameraHorizontalMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Velocidad de movimiento

    void Update()
    {
        // Obtener entrada del jugador
        float horizontal = Input.GetAxis("Horizontal"); // Movimiento a los lados (A/D o flechas)
        float vertical = Input.GetAxis("Vertical"); // Movimiento hacia adelante/atrás (W/S o flechas)

        // Crear vector de movimiento en el plano horizontal
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;

        // Mover la cámara
        transform.Translate(movement, Space.World);
    }
}
