# Player Movement & Camera System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminar o conflito de yaw duplo e implementar controle estilo shooter de terceira pessoa com câmera pivot-based, onde ThirdPersonCamera expõe YawRotation e FireKnightController a consome via SerializeField.

**Architecture:** CameraPivot (filho do Player) herda o yaw do Player e aplica pitch localmente. Camera é filha do CameraPivot e ajusta apenas sua localPosition Z para colisão. FireKnightController lê YawRotation da câmera via propriedade read-only no LateUpdate, garantindo que o yaw da câmera já esteja calculado (Update) antes de ser consumido.

**Tech Stack:** Unity 2022+, C#, Rigidbody, UnityEngine.InputSystem (New Input System), Quaternion.Slerp, Physics.SphereCast

---

## Mapa de Arquivos

| Arquivo | Ação | Responsabilidade |
|---|---|---|
| `Assets/Scenes/Scripts/ThirdPersonCamera.cs` | Reescrever | Yaw/pitch do mouse, exposição de YawRotation, colisão de câmera via SphereCast |
| `Assets/Scenes/Scripts/PlayerController.cs` | Reescrever | Movimento WASD, rotação via YawRotation (Slerp), pulo, dash |
| `Assets/Scenes/Scripts/CombatScript.cs` | Modificar | Cache de FireKnightController em Start() em vez de GetComponent por frame |

---

## Task 1: Reescrever ThirdPersonCamera.cs

**Files:**
- Modify: `Assets/Scenes/Scripts/ThirdPersonCamera.cs`

- [ ] **Step 1: Substituir o conteúdo completo do arquivo**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraPivot;

    [Header("Sensibilidade do Mouse")]
    [SerializeField] private float mouseSensitivity = 0.2f;

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

    private void Start()
    {
        if (target != null)
            currentYaw = target.eulerAngles.y;

        currentDistance = defaultDistance;
    }

    private void Update()
    {
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

        // mouse.delta já é por frame — sem Time.deltaTime para manter FPS-independente
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
```

- [ ] **Step 2: Verificar erros de compilação no IDE**

Abrir `Assets/Scenes/Scripts/ThirdPersonCamera.cs` no IDE e confirmar que não há erros no painel Problems/Errors. Campos esperados no Inspector: `target`, `cameraPivot`, `mouseSensitivity`, `minPitch`, `maxPitch`, `collisionLayers`, `cameraRadius`, `defaultDistance`, `collisionSmoothSpeed`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/Scripts/ThirdPersonCamera.cs
git commit -m "refactor: rewrite ThirdPersonCamera with pivot-based yaw/pitch separation"
```

---

## Task 2: Reescrever PlayerController.cs (FireKnightController)

**Files:**
- Modify: `Assets/Scenes/Scripts/PlayerController.cs`

- [ ] **Step 1: Substituir o conteúdo completo do arquivo**

```csharp
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
    }

    // LateUpdate garante que ThirdPersonCamera.Update() já rodou neste frame
    private void LateUpdate()
    {
        GroundCheck();

        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerMove)
        {
            moveDirection = Vector3.zero;
            isSprinting = false;
            anim.SetBool(HashIsWalking, false);
            anim.SetBool(HashIsRunning, false);
            return;
        }

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
        if (keyboard == null) { moveDirection = Vector3.zero; return; }

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
            isJumping = true;
            anim.SetTrigger(HashJumpTrigger);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
        bool moving = moveDirection.sqrMagnitude > 0.01f && isGrounded && !isJumping;
        anim.SetBool(HashIsWalking, moving && !isSprinting);
        anim.SetBool(HashIsRunning, moving && isSprinting);
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
        rb.linearVelocity = Vector3.zero;

        Vector3 dir = moveDirection.sqrMagnitude > 0.01f ? moveDirection : transform.forward;
        rb.AddForce(dir * force, ForceMode.VelocityChange);
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
```

- [ ] **Step 2: Verificar erros de compilação no IDE**

Confirmar no painel Problems: nenhum erro. Campos esperados no Inspector: `cameraRig` (ThirdPersonCamera), `cameraPivot` (Transform), `walkSpeed`, `runSpeed`, `rotationSpeed`, `jumpForce`, `groundCheck`, `groundDistance`, `groundMask`, `dashForce`, `dashDuration`, `dashCooldown`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/Scripts/PlayerController.cs
git commit -m "refactor: rewrite FireKnightController with LateUpdate yaw from camera and pivot-based movement"
```

---

## Task 3: Cleanup FireKnightCombat (CombatScript.cs)

**Files:**
- Modify: `Assets/Scenes/Scripts/CombatScript.cs`

O `GetComponent<FireKnightController>()` é chamado em `TakeDamage()` (runtime, por frame de dano). Cache em `Start()` para evitar overhead.

- [ ] **Step 1: Adicionar campo privado e cachear em Start()**

Localizar no arquivo:

```csharp
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }
```

Substituir por:

```csharp
    private int currentHealth;
    private FireKnightController playerController;

    private void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<FireKnightController>();
    }
```

- [ ] **Step 2: Usar o campo cacheado em TakeDamage()**

Localizar:

```csharp
        FireKnightController controller = GetComponent<FireKnightController>();
        if (controller != null && controller.isInvincible)
```

Substituir por:

```csharp
        if (playerController != null && playerController.isInvincible)
```

- [ ] **Step 3: Verificar compilação e commit**

```bash
git add Assets/Scenes/Scripts/CombatScript.cs
git commit -m "refactor: cache FireKnightController reference in FireKnightCombat.Start()"
```

---

## Task 4: Configurar Hierarquia na Unity (Setup Manual)

**Nenhum arquivo de script.** Executar dentro do Editor Unity com a cena de gameplay aberta.

- [ ] **Step 1: Criar o GameObject CameraPivot**

No painel Hierarchy:
1. Selecionar o GameObject **Player**
2. Botão direito → **Create Empty** → renomear para `CameraPivot`
3. No Inspector do CameraPivot, definir **Position** local como `(0, 1.6, 0)` e **Rotation** como `(0, 0, 0)`

- [ ] **Step 2: Mover a Main Camera para ser filha do CameraPivot**

No Hierarchy, arrastar **Main Camera** para dentro de **CameraPivot**. Após o drag:
- Main Camera deve aparecer como filha de CameraPivot no Hierarchy
- No Inspector da Main Camera, definir **Position** local como `(0, 0, -5)` e **Rotation** como `(0, 0, 0)`

- [ ] **Step 3: Remover ou desabilitar scripts de câmera antigos**

Se existir algum script de câmera no GameObject Player ou diretamente na Main Camera (que não seja o novo `ThirdPersonCamera`), removê-los agora para evitar conflito.

- [ ] **Step 4: Adicionar ThirdPersonCamera na Main Camera**

1. Selecionar **Main Camera** no Hierarchy
2. No Inspector, clicar **Add Component** → buscar e adicionar `ThirdPersonCamera`
3. Preencher os campos:

| Campo | Valor |
|---|---|
| `target` | Arrastar o GameObject **Player** |
| `cameraPivot` | Arrastar o GameObject **CameraPivot** |
| `mouseSensitivity` | `0.2` |
| `minPitch` | `-80` |
| `maxPitch` | `80` |
| `defaultDistance` | `5` |
| `collisionLayers` | Selecionar layers sólidas (ex: Default, Ground) |
| `cameraRadius` | `0.2` |
| `collisionSmoothSpeed` | `10` |

- [ ] **Step 5: Configurar FireKnightController no Player**

1. Selecionar **Player** no Hierarchy
2. No Inspector, localizar o componente `FireKnightController`
3. Preencher os campos novos:

| Campo | Valor |
|---|---|
| `cameraRig` | Arrastar a **Main Camera** (ou o objeto que contém o ThirdPersonCamera) |
| `cameraPivot` | Arrastar o GameObject **CameraPivot** |
| `rotationSpeed` | `15` |

Garantir que `groundCheck`, `groundMask`, `walkSpeed`, `runSpeed`, etc. continuem configurados como antes.

- [ ] **Step 6: Salvar a cena**

`Ctrl+S` para salvar. Confirmar no Hierarchy que a estrutura está:
```
Player
├── GroundCheck
└── CameraPivot
    └── Main Camera  ← contém ThirdPersonCamera
```

---

## Task 5: Smoke Test em Play Mode

Não há testes automatizados para comportamentos de MonoBehaviour com Rigidbody. Validar manualmente em Play Mode.

- [ ] **Step 1: Entrar em Play Mode e verificar câmera**

Pressionar **Play**. Checklist:
- [ ] Cursor travado e invisível
- [ ] Mover mouse para a direita → câmera gira para direita, Player body gira junto
- [ ] Mover mouse para cima → câmera sobe (pitch), Player NÃO inclina
- [ ] Pitch respeita os limites: não passa de ~80 graus para cima nem para baixo

- [ ] **Step 2: Verificar movimento WASD**

- [ ] W → personagem caminha para frente (direção da câmera)
- [ ] A/D → strafes relativos à câmera
- [ ] S → recua
- [ ] Diagonal (W+A) → move em diagonal, velocidade igual a movimento reto (normalizado)
- [ ] Após girar câmera 90°: W continua sendo "para frente da câmera" (não do mundo)

- [ ] **Step 3: Verificar que não há conflito de rotação**

- [ ] Girar câmera rapidamente: nenhum "salto" ou oscilação no Player
- [ ] Segurar W enquanto gira câmera: Player acompanha câmera suavemente (Slerp)
- [ ] Parar de mover o mouse: Player mantém rotação da câmera

- [ ] **Step 4: Verificar pulo e dash**

- [ ] Space → Player pula, câmera não treme
- [ ] Shift → dash na direção do movimento
- [ ] Alt (no chão) → roll com força reduzida

- [ ] **Step 5: Verificar GameStateManager**

- [ ] Pressionar ESC → jogo pausa, cursor aparece, câmera não recebe input do mouse
- [ ] Pressionar ESC novamente → jogo retorna, cursor trava, câmera volta a funcionar

- [ ] **Step 6: Sair do Play Mode e commitar se tudo OK**

```bash
git add .
git commit -m "test: verify player movement and camera system in play mode"
```

---

## Notas de Ajuste Fino

**Sensibilidade do mouse (`mouseSensitivity`):**
O valor `0.2` representa graus por pixel (sem `Time.deltaTime`). Se a câmera parecer lenta, aumentar para `0.3`–`0.5`. Se parecer rápida demais, reduzir para `0.1`. Ajustar via Inspector em Play Mode para feedback imediato.

**Velocidade de rotação do Player (`rotationSpeed`):**
Valor `15` com `Time.deltaTime` dá rotação em ~7 frames a 60fps. Para rotação mais instantânea, usar `30`+. Para mais suave, `8`–`12`.

**Distância padrão da câmera (`defaultDistance`):**
Valor `5` (em unidades Unity). Ajustar conforme o tamanho do personagem.

**CameraPivot height:**
`(0, 1.6, 0)` funciona para personagens de ~1.8 unidades de altura. Ajustar Y para alinhar com o centro visual do personagem.
