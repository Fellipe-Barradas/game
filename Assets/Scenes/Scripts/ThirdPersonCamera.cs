using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraPivot;

    [Header("Sensibilidade do Mouse")]
    [SerializeField] private float mouseSensitivity = 0.08f;

    [Header("Limites de Pitch")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Colisão da Câmera")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float cameraRadius = 0.2f;
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float collisionSmoothSpeed = 10f;

    private float currentYaw;
    private float currentPitch;
    private float currentDistance;

    // Consumido por FireKnightController no LateUpdate
    public Quaternion YawRotation => Quaternion.Euler(0f, currentYaw, 0f);
    public float CurrentYaw => currentYaw;

    public const string SENSITIVITY_KEY = "MouseSensitivity";

    private void Start()
    {
        if (target != null)
            currentYaw = target.eulerAngles.y;

        if (cameraPivot != null)
            currentPitch = cameraPivot.localEulerAngles.x;

        currentDistance = defaultDistance;
        mouseSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, mouseSensitivity);
    }

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
    }

    private void Update()
    {
        if (cameraPivot == null) return;

        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager == null || stateManager.CanCameraLook)
            ReadMouseInput();

        cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        HandleCameraCollision();
    }

    private void ReadMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Isso permite girar para os lados (Yaw) e para cima/baixo (Pitch)
        currentYaw   += mouse.delta.x.ReadValue() * mouseSensitivity;
        currentPitch -= mouse.delta.y.ReadValue() * mouseSensitivity;
        currentPitch  = Mathf.Clamp(currentPitch, minPitch, maxPitch);
    }
    private void HandleCameraCollision()
    {
        float targetDistance = defaultDistance;

        if (Physics.SphereCast(cameraPivot.position, cameraRadius, -cameraPivot.forward,
                out RaycastHit hit, defaultDistance, collisionLayers))
            targetDistance = Mathf.Max(hit.distance - 0.1f, 0.5f);

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, collisionSmoothSpeed * Time.deltaTime);

        // Posiciona câmera ao longo do eixo local -Z do CameraPivot
        transform.localPosition = new Vector3(0f, 0f, -currentDistance);

        // Câmera olha para o pivot independentemente do pitch herdado
        transform.LookAt(cameraPivot.position);
    }
}
