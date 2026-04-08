using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FireKnightController : MonoBehaviour
{
    [Header("Movimenta��o B�sica")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f; // Segure Ctrl Esquerdo para correr
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

    // GDD: Esquiva (Dash) usa Shift Esq. e deixa invenc�vel durante o per�odo
    public bool isInvincible { get; private set; } = false;
    private bool isDashing = false;
    private float dashTimeCounter;
    private float lastDashTime = -10f;

    // Refer�ncias e Inputs
    private Rigidbody rb;
    private float rotationY;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Trava o cursor e esconde o mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Configura��es obrigat�rias de F�sica via c�digo para evitar erros no Editor
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        // 1. Rota��o do Personagem (Mouse)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        rotationY += mouseX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        // 2. Verifica��o de Ch�o (Ground Check)
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        // 3. Captura de Input de Movimento (W, A, S, D) - GDD
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        // Pega a direção da câmera mas ignora a inclinação vertical (Y)
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = Camera.main.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        // O movimento agora é baseado na visão da câmera!
        moveDirection = (camForward * moveZ + camRight * moveX).normalized;

        // 4. L�gica de Pulo (Espa�o)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isDashing)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Zera o Y antes para pulos consistentes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // 5. L�gica de Esquiva/Dash (Shift Esquerdo) - GDD
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            StartEvasion(dashForce);
        }

        // 6. L�gica de Rolamento (Alt Esquerdo)
        if (Input.GetKeyDown(KeyCode.LeftAlt) && isGrounded && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            // O rolamento compartilha a mesma mec�nica f�sica do Dash, mas pode receber 
            // uma for�a diferente e ser� o local onde voc� engatilhar� a anima��o de rolar.
            StartEvasion(dashForce * 0.8f); // Rolar empurra um pouco menos que o Dash
        }
    }

    void FixedUpdate()
    {
        // Seletor de Estados F�sicos
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
        // Define se est� andando ou correndo (Segurar Ctrl Esq. para correr)
        float currentSpeed = Input.GetKey(KeyCode.LeftControl) ? runSpeed : walkSpeed;

        // Move o Rigidbody calculando a dire��o e velocidade
        Vector3 targetVelocity = moveDirection * currentSpeed;

        // Mant�m a velocidade de queda (eixo Y) intacta para a gravidade funcionar
        targetVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = targetVelocity;
    }

    private void StartEvasion(float appliedForce)
    {
        isDashing = true;
        isInvincible = true; // Aplica invencibilidade mec�nica baseada no GDD
        dashTimeCounter = dashDuration;
        lastDashTime = Time.time;

        // Zera a velocidade atual para a esquiva ter acelera��o instant�nea
        rb.linearVelocity = Vector3.zero;

        // Calcula a dire��o. Se estiver parado, faz o dash para frente da c�mera.
        Vector3 evasionDirection = moveDirection == Vector3.zero ? transform.forward : moveDirection;

        rb.AddForce(evasionDirection * appliedForce, ForceMode.VelocityChange);
    }

    private void HandleEvasion()
    {
        dashTimeCounter -= Time.fixedDeltaTime;

        // Termina a Esquiva/Dash
        if (dashTimeCounter <= 0)
        {
            isDashing = false;
            isInvincible = false; // Retira invencibilidade
            rb.linearVelocity = Vector3.zero; // Freia o personagem para n�o deslizar como gelo
        }
    }
}