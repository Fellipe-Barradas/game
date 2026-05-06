using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerClass
{
    Espadachim,
    Lanceiro,
    Arqueiro
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class FireKnightController : MonoBehaviour
{
    [Header("Classe do Personagem")]
    public PlayerClass currentClass = PlayerClass.Espadachim;

    [Header("Referências de Câmera")]
    [SerializeField] private ThirdPersonCamera cameraRig;
    [SerializeField] private Transform cameraPivot;

    [Header("Movimentação e Pulo")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;
    public float jumpForce = 6f;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Esquiva")]
    public float dashForce = 15f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;
    public bool isInvincible { get; private set; }

    [Header("Combate - Corpo a Corpo (Designers)")]
    public Transform meleeAttackPoint;
    public LayerMask enemyLayers;
    public float swordAttackRange = 2f;
    public float swordAttackWidth = 3f;
    public float lanceAttackRange = 4f; // Dobro do alcance da espada
    public float lanceAttackWidth = 1f; // Menos alcance lateral

    [Header("Combate - À Distância (Designers)")]
    public Transform rangedFirePoint;
    public GameObject projectilePrefab;

    // Privates
    private Rigidbody rb;
    private Animator anim;
    private Vector3 moveDirection;
    private bool isGrounded, isJumping, isDashing, isSprinting;
    private float dashTimeCounter;
    private float lastDashTime = -10f;
    private bool pendingJump, pendingDash;
    private float pendingDashForce;
    private float jumpGraceTimer = 0f;
    private float jumpGraceDuration = 0.15f;
    private float rawMoveX, rawMoveZ;

    // Hashes de Animação
    private static readonly int HashIsJumping = Animator.StringToHash("isJumping");
    private static readonly int HashIsGrounded = Animator.StringToHash("isGrounded");
    private static readonly int HashIsWalking = Animator.StringToHash("isWalking");
    private static readonly int HashIsRunning = Animator.StringToHash("isRunning");
    private static readonly int HashMoveX = Animator.StringToHash("moveX");
    private static readonly int HashMoveZ = Animator.StringToHash("moveZ");
    private static readonly int HashIsDashing = Animator.StringToHash("isDashing");
    
    // Hashes de Ataque
    private static readonly int HashAtaqueEspada = Animator.StringToHash("ataque_espada");
    private static readonly int HashAtaqueLanca = Animator.StringToHash("ataque_lanca");
    private static readonly int HashAtaqueArco = Animator.StringToHash("ataque_arco");

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void LateUpdate()
    {
        // Lógica original de GameStateManager (mantida)
        GroundCheck();

        var keyboard = Keyboard.current;
        isSprinting = keyboard != null && keyboard.leftCtrlKey.isPressed;

        ReadMovementInput(keyboard);
        ApplyYawRotation();
        HandleJump(keyboard);
        HandleDash(keyboard);
        HandleAttack(); 
        UpdateAnimations();
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
            rawMoveX = 0f;
            rawMoveZ = 0f;
            return;
        }

        rawMoveX = 0f;
        rawMoveZ = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  rawMoveX -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) rawMoveX += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  rawMoveZ -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    rawMoveZ += 1f;

        Vector3 forward = cameraPivot.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = cameraPivot.right;   right.y   = 0f; right.Normalize();
        moveDirection = (forward * rawMoveZ + right * rawMoveX).normalized;
    }

    private void ApplyYawRotation()
    {
        if (cameraRig == null) return;
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
            isJumping = true;
            jumpGraceTimer = jumpGraceDuration;
            anim.SetBool(HashIsJumping, true);
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
        float targetX = moving ? rawMoveX * speedMult : 0f;
        float targetZ = moving ? rawMoveZ * speedMult : 0f;

        anim.SetFloat(HashMoveX, Mathf.Lerp(anim.GetFloat(HashMoveX), targetX, Time.deltaTime * 12f));
        anim.SetFloat(HashMoveZ, Mathf.Lerp(anim.GetFloat(HashMoveZ), targetZ, Time.deltaTime * 12f));
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
        anim.SetBool(HashIsDashing, true);
    }

    private void HandleEvasion()
    {
        dashTimeCounter -= Time.fixedDeltaTime;
        if (dashTimeCounter <= 0f)
        {
            isDashing = false;
            isInvincible = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            anim.SetBool(HashIsDashing, false);
        }
    }

    private void HandleAttack()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Aciona a animação correspondente à classe atual
            switch (currentClass)
            {
                case PlayerClass.Espadachim:
                    anim.SetTrigger(HashAtaqueEspada);
                    break;
                case PlayerClass.Lanceiro:
                    anim.SetTrigger(HashAtaqueLanca);
                    break;
                case PlayerClass.Arqueiro:
                    anim.SetTrigger(HashAtaqueArco);
                    break;
            }
        }
    }

    /// <summary>
    /// MÉTODO PARA OS ANIMADORES: 
    /// Adicione um "Animation Event" no frame exato do impacto da espada/lança ou do disparo da flecha 
    /// e chame esta função "ExecuteAttackEvent".
    /// </summary>
    public void ExecuteAttackEvent()
    {
        if (currentClass == PlayerClass.Arqueiro)
        {
            ShootProjectile();
        }
        else
        {
            PerformMeleeAttack();
        }
    }

    private void PerformMeleeAttack()
    {
        if (meleeAttackPoint == null) return;

        Vector3 hitboxSize = currentClass == PlayerClass.Espadachim 
            ? new Vector3(swordAttackWidth, 2f, swordAttackRange) 
            : new Vector3(lanceAttackWidth, 2f, lanceAttackRange);

        // Ajusta o centro do ataque para frente com base no alcance
        Vector3 boxCenter = meleeAttackPoint.position + meleeAttackPoint.forward * (hitboxSize.z / 2f);

        Collider[] hitEnemies = Physics.OverlapBox(boxCenter, hitboxSize / 2f, meleeAttackPoint.rotation, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            Debug.Log($"Acertou o inimigo: {enemy.name} com a classe {currentClass}");
            // Exemplo: enemy.GetComponent<EnemyHealth>().TakeDamage(10);
        }
    }

    private void ShootProjectile()
    {
        if (rangedFirePoint == null || projectilePrefab == null) return;

        Instantiate(projectilePrefab, rangedFirePoint.position, rangedFirePoint.rotation);
    }

    /// <summary>
    /// MÉTODO PARA OS DESIGNERS:
    /// Desenha as caixas de colisão de ataque na tela da Unity para facilitar o balanceamento.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (meleeAttackPoint == null) return;

        Gizmos.color = currentClass == PlayerClass.Espadachim ? Color.red : (currentClass == PlayerClass.Lanceiro ? Color.blue : Color.green);

        if (currentClass == PlayerClass.Espadachim || currentClass == PlayerClass.Lanceiro)
        {
            Vector3 hitboxSize = currentClass == PlayerClass.Espadachim 
                ? new Vector3(swordAttackWidth, 2f, swordAttackRange) 
                : new Vector3(lanceAttackWidth, 2f, lanceAttackRange);

            Vector3 boxCenter = meleeAttackPoint.position + meleeAttackPoint.forward * (hitboxSize.z / 2f);
            
            // Desenha o Hitbox
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, meleeAttackPoint.rotation, hitboxSize);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}