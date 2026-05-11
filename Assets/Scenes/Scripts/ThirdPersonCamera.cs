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

    [Header("Configurações de Mira (Zoom)")] // NOVO BLOCO
    [SerializeField] private float aimDistance = 2f; // Distância mais próxima
    [SerializeField] private Vector3 aimOffset = new Vector3(0.8f, 0f, 0f); // Desloca para a direita (ombro)
    [SerializeField] private float zoomTransitionSpeed = 2f; // Velocidade da transição do zoom

private bool isAimingCam = false; // Controla o estado de zoom

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
    public void SetAimingCamera(bool aiming)
{
    isAimingCam = aiming;
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
    // 1. Define a distância alvo baseada no estado de mira
    float targetDistance = isAimingCam ? aimDistance : defaultDistance;

    // 2. Verifica colisão
    if (Physics.SphereCast(cameraPivot.position, cameraRadius, -cameraPivot.forward,
            out RaycastHit hit, targetDistance, collisionLayers))
    {
        targetDistance = Mathf.Max(hit.distance - 0.1f, 0.5f);
    }

    // 3. Suaviza a distância (Zoom In/Out)
    currentDistance = Mathf.Lerp(currentDistance, targetDistance, collisionSmoothSpeed * Time.deltaTime);

    // 4. Calcula o deslocamento para o ombro (Offset)
    Vector3 targetOffset = isAimingCam ? aimOffset : Vector3.zero;
    
    // 5. Aplica a posição final (Deslocamento X + Zoom no eixo Z local)
    Vector3 desiredLocalPosition = new Vector3(targetOffset.x, targetOffset.y, -currentDistance);
    transform.localPosition = Vector3.Lerp(transform.localPosition, desiredLocalPosition, zoomTransitionSpeed * Time.deltaTime);

    // 6. Faz a câmera olhar para o pivot (ajustado pelo offset para não entortar a visão)
    transform.LookAt(cameraPivot.position + transform.right * targetOffset.x);
}
}
