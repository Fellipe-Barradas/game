using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Alvo")]
    public Transform target; // Arraste o Player para aqui
    
    [Header("Configurações de Distância")]
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Posição padrão atrás do player
    public float smoothSpeed = 10f; // Suavidade do seguimento
    
    [Header("Rotação e Limites")]
    public float mouseSensitivity = 3f;
    public float minPitch = -20f; // Limite para olhar para baixo
    public float maxPitch = 45f;  // Limite para olhar para cima
    
    [Header("Colisão da Câmera")]
    public LayerMask collisionLayers; // Marque a layer "Ground" e "Walls"
    public float cameraRadius = 0.2f; // Raio para detectar colisão
    
    private float currentYaw;   // Rotação horizontal (Eixo Y)
    private float currentPitch; // Rotação vertical (Eixo X)

    void Start()
    {
        // Garante que a câmera comece na posição correta
        currentYaw = target.eulerAngles.y;
        
        // Se o cursor não estiver travado, descomente a linha abaixo:
        // Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate() // LateUpdate é melhor para câmeras para evitar trepidação
    {
        if (!target) return;

        // 1. Captura do Input do Mouse para Orbitar
        currentYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch); // Limites verticais

        // 2. Calcula a Rotação Desejada
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // 3. Calcula a Posição Desejada (Base + Offset Rotacionado)
        Vector3 desiredPosition = target.position + rotation * offset;

        // 4. Sistema de Colisão (Evitar atravessar paredes)
        desiredPosition = HandleCameraCollision(target.position + Vector3.up * offset.y, desiredPosition);

        // 5. Aplica Posição e Rotação com Suavidade
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * offset.y);
    }

    private Vector3 HandleCameraCollision(Vector3 targetCenter, Vector3 desiredPos)
    {
        RaycastHit hit;
        Vector3 direction = desiredPos - targetCenter;
        float distance = direction.magnitude;

        // Dispara uma esfera do centro do player até a câmera para ver se bate em algo
        if (Physics.SphereCast(targetCenter, cameraRadius, direction.normalized, out hit, distance, collisionLayers))
        {
            // Se bater, move a câmera para o ponto de impacto (com um pequeno recuo)
            return targetCenter + direction.normalized * (hit.distance - 0.1f);
        }

        return desiredPos;
    }
}