using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class FireKnightController : MonoBehaviour
{
    [Header("Movimentação Básica")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float mouseSensitivity = 200f;

    [Header("Pulo")]
    public float jumpForce = 6f;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("Esquiva (Dash / Roll)")]
    public float dashForce = 15f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;

    public bool isInvincible { get; private set; } = false;
    private bool isDashing = false;
    private float dashTimeCounter;
    private float lastDashTime = -10f;

    private Rigidbody rb;
    private Animator anim;
    private float rotationY;
    private Vector3 moveDirection;
    private bool isJumping = false;

    private static readonly int HashJumpTrigger = Animator.StringToHash("jumpTrigger");
    private static readonly int HashIsWalking   = Animator.StringToHash("isWalking");
    private static readonly int HashIsRunning   = Animator.StringToHash("isRunning");

    private void Start()
    {
        rb   = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        rb.freezeRotation         = true;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse    = Mouse.current;

        // 1. Rotação do Personagem (Mouse)
        float mouseX = mouse != null ? mouse.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime : 0f;
        rotationY += mouseX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        // 2. Ground Check
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (isJumping && isGrounded)
            isJumping = false;

        // 3. Input de Movimento
        float moveX = 0f;
        float moveZ = 0f;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  moveX -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  moveZ -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    moveZ += 1f;
        }

        Vector3 camForward = Camera.main.transform.forward; camForward.y = 0; camForward.Normalize();
        Vector3 camRight   = Camera.main.transform.right;   camRight.y   = 0; camRight.Normalize();

        moveDirection = (camForward * moveZ + camRight * moveX).normalized;

        // 4. Animações de movimento
        bool isRunKey = keyboard != null && keyboard.leftCtrlKey.isPressed;

        bool isMoving  = moveDirection.sqrMagnitude > 0.01f;
        bool isRunning = isMoving && isRunKey && isGrounded && !isJumping;
        bool isWalking = isMoving && !isRunKey && isGrounded && !isJumping;

        anim.SetBool(HashIsWalking, isWalking);
        anim.SetBool(HashIsRunning, isRunning);

        // 5. Pulo
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && isGrounded && !isDashing)
        {
            isJumping = true;
            anim.SetTrigger(HashJumpTrigger);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // 6. Dash
        if (keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame && Time.time >= lastDashTime + dashCooldown && !isDashing)
            StartEvasion(dashForce);

        // 7. Roll
        if (keyboard != null && keyboard.leftAltKey.wasPressedThisFrame && isGrounded && Time.time >= lastDashTime + dashCooldown && !isDashing)
            StartEvasion(dashForce * 0.8f);
    }

    private void FixedUpdate()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isDashing) HandleEvasion();
        else           MovePlayer();
    }

    private void MovePlayer()
    {
        var keyboard = Keyboard.current;
        float currentSpeed     = (keyboard != null && keyboard.leftCtrlKey.isPressed) ? runSpeed : walkSpeed;
        Vector3 targetVelocity = moveDirection * currentSpeed;
        targetVelocity.y       = rb.linearVelocity.y;
        rb.linearVelocity      = targetVelocity;
    }

    private void StartEvasion(float appliedForce)
    {
        isDashing        = true;
        isInvincible     = true;
        dashTimeCounter  = dashDuration;
        lastDashTime     = Time.time;
        rb.linearVelocity = Vector3.zero;

        Vector3 evasionDirection = moveDirection == Vector3.zero ? transform.forward : moveDirection;
        rb.AddForce(evasionDirection * appliedForce, ForceMode.VelocityChange);
    }

    private void HandleEvasion()
    {
        dashTimeCounter -= Time.fixedDeltaTime;
        if (dashTimeCounter <= 0f)
        {
            isDashing         = false;
            isInvincible      = false;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
