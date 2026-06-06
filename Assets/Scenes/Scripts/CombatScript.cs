using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CombatScript : MonoBehaviour
{
    [Header("Status de Upgrade")]
    public int bonusDanoUpgrade = 0;
    [HideInInspector] public float tierDamageMultiplier = 1f;
    [Header("Arma Equipada")]
    public WeaponData currentWeapon;
    
    [Header("Ossos das Mãos")]
    public Transform rightHandHolder; // Arraste a mão Direita (Espada/Lança)
    public Transform leftHandHolder;  // Arraste a mão Esquerda (Arco)

    [Header("Configuração de Acerto — Corpo a Corpo")]
    public Transform meleeAttackPoint;
    public LayerMask enemyLayers;
    public float swordAttackRange = 2f;
    public float swordAttackWidth = 3f;
    public float lanceAttackRange = 4f;
    public float lanceAttackWidth = 1f;

    [Header("Configuração de Acerto — Distância")]
    public Transform rangedFirePoint;
    public GameObject projectilePrefab;

    [Header("Feedbacks Visuais")]
    public ParticleSystem hitSparks;

    [Header("Sons")]
    public AudioSource audioSource;
    public AudioClip blockSound;
    public AudioClip hurtSound;
    [SerializeField] private AudioMixerGroup playerAudioGroup;

    [Header("Mira e Câmera (Arqueiro)")]
    public GameObject crosshairUI; // Arraste a UI da mira aqui
    public ThirdPersonCamera camScript; // Arraste a Câmera Principal aqui

    public Image chargeBarFill; // NOVO: A imagem que vai "encher"
    public CanvasGroup crosshairCanvasGroup; // NOVO: Para fazer a mira aparecer suavemente

    [Header("Configuração de Carga do Arco")]
    public float bowChargeDuration = 1.2f;
    private float currentChargeTime = 0f;

    public bool isBlocking { get; private set; }

    private float nextAttackTime;
    private bool isChargingShot = false;
    private Vector3 storedShotDirection;
    private Animator anim;
    private FireKnightController controller;

    private void Start()
    {
        controller = GetComponent<FireKnightController>();
        anim = GetComponent<Animator>();

        if (audioSource != null && playerAudioGroup != null)
            audioSource.outputAudioMixerGroup = playerAudioGroup;

        // 1. Esconde a UI da mira por padrão
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (crosshairCanvasGroup != null) crosshairCanvasGroup.alpha = 0f;

        CriarChargeBarSeNecessario();

        // 2. PEGA A ARMA DO MENU
        if (GameStateManager.Instance != null && GameStateManager.Instance.SelectedWeapon != null)
        {
            currentWeapon = GameStateManager.Instance.SelectedWeapon;
        }

        // 3. CRIA O MODELO 3D NA MÃO
        if (currentWeapon != null && currentWeapon.weaponPrefab != null)
        {
            // Descobre qual mão usar com base na configuração da arma
            Transform targetHand = currentWeapon.equipInLeftHand ? leftHandHolder : rightHandHolder;

            if (targetHand != null)
            {
                // Instancia na mão escolhida
                GameObject arma = Instantiate(currentWeapon.weaponPrefab, targetHand, false);
                
                arma.transform.localScale = Vector3.one;

                // Aplica a "pegada" com precisão
                arma.transform.localPosition = currentWeapon.handPositionOffset;
                arma.transform.localRotation = Quaternion.Euler(currentWeapon.handRotationOffset);
            }
            else
            {
                Debug.LogWarning("O osso da mão não foi referenciado no CombatScript!");
            }
        }
    }
    
    private void CriarChargeBarSeNecessario()
    {
        if (chargeBarFill != null || crosshairUI == null) return;

        // Fundo escuro — filho do crosshairUI para sumir/aparecer junto
        GameObject fundo = new GameObject("ChargeBar_BG");
        fundo.transform.SetParent(crosshairUI.transform, false);
        RectTransform fundoRect = fundo.AddComponent<RectTransform>();
        fundoRect.anchorMin = new Vector2(0.5f, 0.5f);
        fundoRect.anchorMax = new Vector2(0.5f, 0.5f);
        fundoRect.sizeDelta = new Vector2(120f, 12f);
        fundoRect.anchoredPosition = new Vector2(0f, -50f);
        Image fundoImg = fundo.AddComponent<Image>();
        fundoImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Preenchimento amarelo — largura controlada por anchorMax.x (0 = vazio, 1 = cheio)
        GameObject fill = new GameObject("ChargeBar_Fill");
        fill.transform.SetParent(fundo.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f); // começa com largura 0
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        chargeBarFill = fill.AddComponent<Image>();
        chargeBarFill.color = new Color(1f, 0.85f, 0f);
    }

    private void Update()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerAct)
        {
            isBlocking = false;
            CancelAim();
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (controller != null && controller.currentClass == PlayerClass.Arqueiro)
        {
            HandleArcherInput(mouse);
            return;
        }

        isBlocking = mouse.rightButton.isPressed;

        if (!isBlocking && mouse.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= nextAttackTime)
            {
                float rate     = currentWeapon != null ? currentWeapon.attackRate : 1f;
                nextAttackTime = Time.time + 1f / rate;
                Attack();
            }
        }
    }

  private void HandleArcherInput(Mouse mouse)
    {
        // 1. INÍCIO DA MIRA (Clica)
        if (mouse.leftButton.wasPressedThisFrame && !isChargingShot && Time.time >= nextAttackTime)
        {
            isChargingShot = true;
            currentChargeTime = 0f; // Começa a carga do zero
            controller.SetAiming(true);

            if (crosshairUI != null) crosshairUI.SetActive(true);
            if (crosshairCanvasGroup != null) crosshairCanvasGroup.alpha = 1f;
            if (chargeBarFill != null) chargeBarFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            
            if (camScript != null) camScript.SetAimingCamera(true);

            if (audioSource != null && currentWeapon?.swingSound != null)
                audioSource.PlayOneShot(currentWeapon.swingSound);
        }

        // 2. SEGURANDO O BOTÃO (Carregando)
        if (isChargingShot)
        {
            currentChargeTime += Time.deltaTime;

            if (chargeBarFill != null)
            {
                float t = Mathf.Clamp01(currentChargeTime / bowChargeDuration);
                chargeBarFill.rectTransform.anchorMax = new Vector2(t, 1f);
            }
        }

        // 3. SOLTA O BOTÃO (Momento de decisão)
        if (mouse.leftButton.wasReleasedThisFrame && isChargingShot)
        {
            // O jogador só pode atirar SE completou o tempo da barra
            if (currentChargeTime >= bowChargeDuration)
            {
                ExecuteShot();
            }
            else
            {
                // Se soltou antes de encher, o tiro é cancelado!
                CancelAim();
            }
        }

        // 4. CANCELAR (Botão Direito)
        if (mouse.rightButton.wasPressedThisFrame && isChargingShot)
            CancelAim();
    }

    private void ExecuteShot()
    {
        // Guarda a direção AGORA, enquanto a câmera ainda está na posição de mira
        if (rangedFirePoint != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int mask = ~LayerMask.GetMask("player");
            storedShotDirection = Physics.Raycast(ray, out RaycastHit hit, 100f, mask)
                ? (hit.point - rangedFirePoint.position).normalized
                : ray.direction;
        }

        isChargingShot = false;
        controller.SetAiming(false);

        // Esconde UI e Zoom
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (camScript != null) camScript.SetAimingCamera(false);
        
        float rate = currentWeapon != null ? currentWeapon.attackRate : 1f;
        float animDuration = 1f / rate;
        
        controller.TriggerAttackAnimation();
        nextAttackTime = Time.time + animDuration; // Cooldown após atirar
    }

    private void CancelAim()
    {
        if (!isChargingShot) return;
        isChargingShot = false;
        currentChargeTime = 0f;
        controller.SetAiming(false);
        
        // Esconde UI e Zoom
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (camScript != null) camScript.SetAimingCamera(false);
    }

    private void Attack()
    {
        if (controller != null)
            controller.TriggerAttackAnimation();

        if (audioSource != null && currentWeapon?.swingSound != null)
            audioSource.PlayOneShot(currentWeapon.swingSound);
    }

    public void ExecuteAttackEvent()
    {
        if (controller == null) return;

        if (controller.currentClass == PlayerClass.Arqueiro){
            ShootProjectile();
        }
        else{
            PerformMeleeAttack();
        }
    }

    private void PerformMeleeAttack()
    {
        if (meleeAttackPoint == null) return;
        Vector3 hitboxSize = controller.currentClass == PlayerClass.Espadachim
            ? new Vector3(swordAttackWidth, 2f, swordAttackRange)
            : new Vector3(lanceAttackWidth, 2f, lanceAttackRange);
        Debug.Log($"{hitboxSize} criada");
        Vector3 boxCenter = meleeAttackPoint.position + meleeAttackPoint.forward * (hitboxSize.z / 2f);
        Debug.Log($"{boxCenter} criada");
        Debug.Log($"{Physics.OverlapBox(boxCenter, hitboxSize / 2f, meleeAttackPoint.rotation, enemyLayers)} criada");
        Collider[] hits = Physics.OverlapBox(boxCenter, hitboxSize / 2f, meleeAttackPoint.rotation, enemyLayers);

        Debug.Log($"[MELEE ATTACK] Acertou {hits.Length} inimigo(s)!");

        int damage = (int)((currentWeapon != null ? currentWeapon.attackDamage : 10) * tierDamageMultiplier) + bonusDanoUpgrade;

        foreach (Collider enemy in hits)
        {
            Debug.Log($"[HIT] Inimigo atingido: {enemy.name} - Dano: {damage}");
            
            if (audioSource != null && currentWeapon?.hitSound != null)
                audioSource.PlayOneShot(currentWeapon.hitSound);

            if (hitSparks != null)
                Instantiate(hitSparks, enemy.ClosestPoint(transform.position), Quaternion.identity);

            if (enemy.TryGetComponent<EnemyDummy>(out EnemyDummy e))
                e.TakeDamage(damage);
            if (enemy.TryGetComponent<BossEnemy>(out BossEnemy boss))
                boss.TakeDamage(damage);    
        }
    }

    private void ShootProjectile()
    {
        if (rangedFirePoint == null || projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, rangedFirePoint.position, Quaternion.LookRotation(storedShotDirection));

        // Passa o dano para a flecha (AQUI É A MUDANÇA)
        ProjectileScript projScript = projectile.GetComponent<ProjectileScript>();
        if (projScript != null)
        {
            // Somamos o dano da arma ao bônus acumulado pelos upgrades
            projScript.damage = (int)((currentWeapon != null ? currentWeapon.attackDamage : 10) * tierDamageMultiplier) + bonusDanoUpgrade;
        }
    }
    public void TakeDamage(int damage)
    {
        if (isBlocking)
        {
            if (audioSource != null && blockSound != null)
                audioSource.PlayOneShot(blockSound);
            Debug.Log("Dano bloqueado!");
            return;
        }

        if (controller != null && controller.isInvincible) return;

        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);
        
        if (anim != null)
        {
            anim.SetTrigger("TakeDamage");
        }

        PlayerHealth scriptDeVida = GetComponent<PlayerHealth>();
        if (scriptDeVida != null)
        {
            scriptDeVida.ReceberDano(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (meleeAttackPoint == null || controller == null) return;

        Gizmos.color = controller.currentClass switch
        {
            PlayerClass.Espadachim => Color.red,
            PlayerClass.Lanceiro   => Color.blue,
            _                      => Color.green
        };

        if (controller.currentClass == PlayerClass.Arqueiro) return;

        Vector3 hitboxSize = controller.currentClass == PlayerClass.Espadachim
            ? new Vector3(swordAttackWidth, 2f, swordAttackRange)
            : new Vector3(lanceAttackWidth, 2f, lanceAttackRange);

        Vector3 boxCenter = meleeAttackPoint.position + meleeAttackPoint.forward * (hitboxSize.z / 2f);

        Gizmos.matrix = Matrix4x4.TRS(boxCenter, meleeAttackPoint.rotation, hitboxSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}