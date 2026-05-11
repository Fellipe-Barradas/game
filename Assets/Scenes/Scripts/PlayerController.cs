using UnityEngine;
using UnityEngine.Animations.Rigging; // Adicione isso lá em cima
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class FireKnightController : MonoBehaviour
{
    public PlayerClass currentClass = PlayerClass.Arqueiro;
    [Header("Referências de Câmera")]
    [SerializeField] private ThirdPersonCamera cameraRig;
    [SerializeField] private Transform cameraPivot;

    [Header("Movimentação e Pulo")]
    public float walkSpeed      = 5f;
    public float runSpeed       = 8f;
    public float aimSpeed       = 2f;
    [SerializeField] private float rotationSpeed = 15f;
    public float jumpForce      = 6f;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Configuração do Tiro")]
    [Tooltip("Quanto tempo o peito continua mirando APÓS soltar a flecha")]
    public float tempoSegurandoRig = 1.0f;

    [Header("Esquiva")]
    public float dashForce    = 15f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;
    public bool isInvincible { get; private set; }

    [Header("Mira com Rigging")]
    public Rig rig; 
    public float rigBlendSpeed = 10f;
    
    // Privates
    private Rigidbody rb;
    private Animator  anim;
    private Vector3   moveDirection;
    private bool isGrounded, isJumping, isDashing, isSprinting;
    private float dashTimeCounter;
    private float lastDashTime      = -10f;
    private bool  pendingJump, pendingDash;
    private float pendingDashForce;
    private float jumpGraceTimer    = 0f;
    private float jumpGraceDuration = 0.15f;
    private float rawMoveX, rawMoveZ;
    private bool isAttacking = false; // Variável movida para cá para organizar

    // Hashes — Base Layer
    private static readonly int HashIsJumping  = Animator.StringToHash("isJumping");
    private static readonly int HashIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int HashIsWalking  = Animator.StringToHash("isWalking");
    private static readonly int HashIsRunning  = Animator.StringToHash("isRunning");
    private static readonly int HashMoveX      = Animator.StringToHash("moveX");
    private static readonly int HashMoveZ      = Animator.StringToHash("moveZ");
    private static readonly int HashIsDashing  = Animator.StringToHash("isDashing");

    // Hashes — Upper Body Layer
    private static readonly int HashClassIndex    = Animator.StringToHash("classIndex");
    private static readonly int HashAttackTrigger = Animator.StringToHash("attackTrigger");
    private static readonly int HashIsAiming      = Animator.StringToHash("isAiming");

    // 1. Propriedade para rastrear o estado de mira
    public bool isAimingState { get; private set; }

    // Métodos públicos para o Combat chamar
   public void TriggerAttackAnimation() 
    {
        isAttacking = true;
        anim.SetTrigger(HashAttackTrigger);
        
        // Cancela qualquer timer antigo para não bugar
        CancelInvoke(nameof(ResetAttackState)); 
        
        // Desliga o isAttacking exatamente após o tempo que você configurou!
        Invoke(nameof(ResetAttackState), tempoSegurandoRig); 
    }
    private void ResetAttackState() => isAttacking = false;

    // 2. Atualize o seu método SetAiming
    public void SetAiming(bool value)
    {
        isAimingState = value;
        anim.SetBool(HashIsAiming, value);
    }

    private void Start()
    {
        rb   = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation         = true;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (cameraRig == null)   Debug.LogError("FireKnightController: cameraRig not assigned.",    this);
        if (cameraPivot == null) Debug.LogError("FireKnightController: cameraPivot not assigned.",  this);
        if (groundCheck == null) Debug.LogWarning("FireKnightController: groundCheck not assigned.", this);

        ApplyClass();
    }

    private void ApplyClass()
    {
        if (GameStateManager.Instance != null)
            currentClass = GameStateManager.Instance.SelectedClass;

        anim.SetInteger(HashClassIndex, (int)currentClass);
    }

    private void LateUpdate()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            moveDirection = Vector3.zero;
            rawMoveX = rawMoveZ = 0f;
            isSprinting = false;
            anim.SetBool(HashIsWalking, false);
            anim.SetBool(HashIsRunning, false);
            anim.SetFloat(HashMoveX, 0f);
            anim.SetFloat(HashMoveZ, 0f);
            return;
        }

        GroundCheck();

        var keyboard = Keyboard.current;
        isSprinting  = keyboard != null && keyboard.leftCtrlKey.isPressed;

        ReadMovementInput(keyboard);
        ApplyYawRotation();
        HandleJump(keyboard);
        HandleDash(keyboard);
        UpdateAnimations();
        
        // Chamada adicionada para processar o Rigging
        UpdateRigWeight();
    }

    private void GroundCheck()
    {
        if (jumpGraceTimer > 0f)
        {
            jumpGraceTimer -= Time.deltaTime;
            isGrounded = false;
            anim.SetBool(HashIsGrounded, false);
            return;
        }

        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        anim.SetBool(HashIsGrounded, isGrounded);

        if (isJumping && isGrounded)
        {
            isJumping = false;
            anim.SetBool(HashIsJumping, false);
        }
    }

    private void ReadMovementInput(Keyboard keyboard)
    {
        if (keyboard == null || cameraPivot == null)
        {
            moveDirection = Vector3.zero;
            rawMoveX = rawMoveZ = 0f;
            return;
        }

        rawMoveX = rawMoveZ = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  rawMoveX -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) rawMoveX += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  rawMoveZ -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    rawMoveZ += 1f;

        Vector3 forward = cameraPivot.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = cameraPivot.right;   right.y   = 0f; right.Normalize();
        moveDirection   = (forward * rawMoveZ + right * rawMoveX).normalized;
    }

    // Modifique a rotação para ser imediata se estiver atirando
    private void ApplyYawRotation()
    {
        if (cameraRig == null) return;

        // Se estiver atacando ou mirando, usa a rotação horizontal da câmera (Yaw)
        // Multiplicamos por 2f para o corpo acompanhar o mouse mais rapidamente durante a mira
        if (isAttacking || anim.GetBool(HashIsAiming))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, cameraRig.YawRotation, rotationSpeed * 2f * Time.deltaTime);
        }
        else
        {
            // Rotação normal quando está apenas andando
            transform.rotation = Quaternion.Slerp(transform.rotation, cameraRig.YawRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJump(Keyboard keyboard)
    {
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && isGrounded && !isDashing)
        {
            pendingJump    = true;
            isJumping      = true;
            jumpGraceTimer = jumpGraceDuration;
            anim.SetBool(HashIsJumping,  true);
            anim.SetBool(HashIsGrounded, false);
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

        float speedMult = isSprinting ? 2f : 1f;
        float targetX   = moving ? rawMoveX * speedMult : 0f;
        float targetZ   = moving ? rawMoveZ * speedMult : 0f;

        anim.SetFloat(HashMoveX, Mathf.Lerp(anim.GetFloat(HashMoveX), targetX, Time.deltaTime * 12f));
        anim.SetFloat(HashMoveZ, Mathf.Lerp(anim.GetFloat(HashMoveZ), targetZ, Time.deltaTime * 12f));
    }

    // Método responsável por controlar o peso do Rigging
    private void UpdateRigWeight()
    {
        if (rig == null) return;

        // Se estiver mirando ou atirando, o peso vai para 1. Senão, vai para 0.
        float targetWeight = (isAimingState || isAttacking) ? 1f : 0f;
        
        rig.weight = Mathf.Lerp(rig.weight, targetWeight, Time.deltaTime * rigBlendSpeed);
    }

    private void FixedUpdate()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            pendingJump = pendingDash = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (pendingJump)
        {
            pendingJump = false;
            isJumping   = true;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (pendingDash)
        {
            pendingDash       = false;
            rb.linearVelocity = Vector3.zero;
            Vector3 dir = moveDirection.sqrMagnitude > 0.01f ? moveDirection : transform.forward;
            rb.AddForce(dir * pendingDashForce, ForceMode.VelocityChange);
        }

        if (isDashing) HandleEvasion();
        else           MovePlayer();
    }

    private void MovePlayer()
    {
        float speed = isAimingState ? aimSpeed : (isSprinting ? runSpeed : walkSpeed);
    
        rb.linearVelocity = new Vector3(
            moveDirection.x * speed,
            rb.linearVelocity.y,
            moveDirection.z * speed
        );
    }

    private void StartEvasion(float force)
    {
        isDashing        = true;
        isInvincible     = true;
        dashTimeCounter  = dashDuration;
        lastDashTime     = Time.time;
        pendingDash      = true;
        pendingDashForce = force;
        anim.SetBool(HashIsDashing, true);
    }

    private void HandleEvasion()
    {
        dashTimeCounter -= Time.fixedDeltaTime;
        if (dashTimeCounter <= 0f)
        {
            isDashing    = false;
            isInvincible = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            anim.SetBool(HashIsDashing, false);
        }
    }
}