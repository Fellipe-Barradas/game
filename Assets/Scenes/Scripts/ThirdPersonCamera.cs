using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Alvo")]
    public Transform target;

    [Header("Configuracoes de Distancia")]
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    public float smoothSpeed = 10f;

    [Header("Rotacao e Limites")]
    public float mouseSensitivity = 3f;
    public float minPitch = -20f;
    public float maxPitch = 45f;

    [Header("Colisao da Camera")]
    public LayerMask collisionLayers;
    public float cameraRadius = 0.2f;

    private float currentYaw;
    private float currentPitch;

    private void Start()
    {
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        if (!target)
        {
            return;
        }

        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager == null || stateManager.CanCameraLook)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                currentYaw   += mouse.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
                currentPitch -= mouse.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;
            }
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = target.position + rotation * offset;
        desiredPosition = HandleCameraCollision(target.position + Vector3.up * offset.y, desiredPosition);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * offset.y);
    }

    private Vector3 HandleCameraCollision(Vector3 targetCenter, Vector3 desiredPos)
    {
        Vector3 direction = desiredPos - targetCenter;
        float distance = direction.magnitude;

        if (Physics.SphereCast(targetCenter, cameraRadius, direction.normalized, out RaycastHit hit, distance, collisionLayers))
        {
            return targetCenter + direction.normalized * (hit.distance - 0.1f);
        }

        return desiredPos;
    }
}
