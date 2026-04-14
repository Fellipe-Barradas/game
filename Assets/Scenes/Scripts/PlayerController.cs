using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FireKnightController : MonoBehaviour
{
    [Header("Movimentacao Basica")]
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

    public bool isInvincible { get; private set; }

    private bool isDashing;
    private float dashTimeCounter;
    private float lastDashTime = -10f;
    private Rigidbody rb;
    private float rotationY;
    private Vector3 moveDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            moveDirection = Vector3.zero;
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        rotationY += mouseX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = mainCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        moveDirection = (camForward * moveZ + camRight * moveX).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isDashing)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            StartEvasion(dashForce);
        }

        if (Input.GetKeyDown(KeyCode.LeftAlt) && isGrounded && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            StartEvasion(dashForce * 0.8f);
        }
    }

    private void FixedUpdate()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isDashing)
        {
            HandleEvasion();
        }
        else
        {
            MovePlayer();
        }
    }

    private void MovePlayer()
    {
        float currentSpeed = Input.GetKey(KeyCode.LeftControl) ? runSpeed : walkSpeed;
        Vector3 targetVelocity = moveDirection * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    private void StartEvasion(float appliedForce)
    {
        isDashing = true;
        isInvincible = true;
        dashTimeCounter = dashDuration;
        lastDashTime = Time.time;
        rb.linearVelocity = Vector3.zero;

        Vector3 evasionDirection = moveDirection == Vector3.zero ? transform.forward : moveDirection;
        rb.AddForce(evasionDirection * appliedForce, ForceMode.VelocityChange);
    }

    private void HandleEvasion()
    {
        dashTimeCounter -= Time.fixedDeltaTime;

        if (dashTimeCounter <= 0f)
        {
            isDashing = false;
            isInvincible = false;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
