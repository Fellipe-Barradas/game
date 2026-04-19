using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class FireKnightController : MonoBehaviour
{
    [Header("Referências de Câmera")]
    [SerializeField] private ThirdPersonCamera cameraRig;
    [SerializeField] private Transform cameraPivot;

    [Header("Movimentação")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Pulo")]
    public float jumpForce = 6f;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Esquiva")]
    public float dashForce = 15f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;

    public bool isInvincible { get; private set; }

    private Rigidbody rb;
    private Animator anim;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isJumping;
    private bool isDashing;
    private bool isSprinting;
    private float dashTimeCounter;
    private float lastDashTime = -10f;
    private bool pendingJump;
    private bool pendingDash;
    private float pendingDashForce;

    private static readonly int HashJumpTrigger = Animator.StringToHash("jumpTrigger");
    private static readonly int HashIsWalking   = Animator.StringToHash("isWalking");
    private static readonly int HashIsRunning   = Animator.StringToHash("isRunning");

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (cameraRig == null)   Debug.LogError("FireKnightController: cameraRig not assigned.", this);
        if (cameraPivot == null) Debug.LogError("FireKnightController: cameraPivot not assigned.", this);
        if (groundCheck == null) Debug.LogWarning("FireKnightController: groundCheck not assigned.", this);
    }

    // LateUpdate garante que ThirdPersonCamera.Update() já rodou neste frame
    private void LateUpdate()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            moveDirection = Vector3.zero;
            isSprinting = false;
            anim.SetBool(HashIsWalking, false);
            anim.SetBool(HashIsRunning, false);
            return;
        }

        GroundCheck();

        var keyboard = Keyboard.current;
        isSprinting = keyboard != null && keyboard.leftCtrlKey.isPressed;

        ReadMovementInput(keyboard);
        ApplyYawRotation();
        HandleJump(keyboard);
        HandleDash(keyboard);
        UpdateAnimations();
    }

    private void GroundCheck()
    {
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isJumping && isGrounded)
            isJumping = false;
    }

    private void ReadMovementInput(Keyboard keyboard)
    {
        if (keyboard == null || cameraPivot == null) { moveDirection = Vector3.zero; return; }

        float moveX = 0f, moveZ = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  moveX -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  moveZ -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    moveZ += 1f;

        // Zerar Y e normalizar para evitar movimento inclinado
        Vector3 forward = cameraPivot.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = cameraPivot.right;   right.y   = 0f; right.Normalize();
        moveDirection = (forward * moveZ + right * moveX).normalized;
    }

    private void ApplyYawRotation()
    {
        if (cameraRig == null) return;

        // Estilo shooter: player sempre alinhado com a câmera
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            cameraRig.YawRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void HandleJump(Keyboard keyboard)
    {
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && isGrounded && !isDashing)
        {
            pendingJump = true;
            anim.SetTrigger(HashJumpTrigger);
        }
    }

    private void HandleDash(Keyboard keyboard)
    {
        if (keyboard == null) return;
        bool cooldownReady = Time.time >= lastDashTime + dashCooldown && !isDashing;
        if (keyboard.leftShiftKey.wasPressedThisFrame && cooldownReady)
            StartEvasion(dashForce);
        if (keyboard.leftAltKey.wasPressedThisFrame && isGrounded && cooldownReady)
            StartEvasion(dashForce * 0.8f);
    }

    private void UpdateAnimations()
    {
        bool moving = moveDirection.sqrMagnitude > 0.01f && isGrounded && !isJumping && !isDashing;
        anim.SetBool(HashIsWalking, moving && !isSprinting);
        anim.SetBool(HashIsRunning, moving && isSprinting);
    }

    private void FixedUpdate()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            pendingJump = false;
            pendingDash = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (pendingJump)
        {
            pendingJump = false;
            isJumping = true;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (pendingDash)
        {
            pendingDash = false;
            rb.linearVelocity = Vector3.zero;
            Vector3 dir = moveDirection.sqrMagnitude > 0.01f ? moveDirection : transform.forward;
            rb.AddForce(dir * pendingDashForce, ForceMode.VelocityChange);
        }

        if (isDashing) HandleEvasion();
        else           MovePlayer();
    }

    private void MovePlayer()
    {
        float speed = isSprinting ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector3(
            moveDirection.x * speed,
            rb.linearVelocity.y,
            moveDirection.z * speed
        );
    }

    private void StartEvasion(float force)
    {
        isDashing = true;
        isInvincible = true;
        dashTimeCounter = dashDuration;
        lastDashTime = Time.time;
        pendingDash = true;
        pendingDashForce = force;
    }

    private void HandleEvasion()
    {
        dashTimeCounter -= Time.fixedDeltaTime;
        if (dashTimeCounter <= 0f)
        {
            isDashing = false;
            isInvincible = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }
}
