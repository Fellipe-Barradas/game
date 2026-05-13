using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CombatScript : MonoBehaviour
{
    [Header("Status de Upgrade")]
    public int bonusDanoUpgrade = 0;
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

    [Header("Mira e Câmera (Arqueiro)")]
    public GameObject crosshairUI; // Arraste a UI da mira aqui
    public ThirdPersonCamera camScript; // Arraste a Câmera Principal aqui

    public Image chargeBarFill; // NOVO: A imagem que vai "encher"
    public CanvasGroup crosshairCanvasGroup; // NOVO: Para fazer a mira aparecer suavemente

    [Header("Configuração de Carga do Arco")]
    public float bowChargeDuration = 1.2f; // Tempo em segundos para a barra encher 100%
    public float fadeSpeed = 5f; // Velocidade em que a mira surge na tela
    private float currentChargeTime = 0f;

    public bool isBlocking { get; private set; }

    private float nextAttackTime;
    private bool isChargingShot = false;
    private Animator anim;
    private FireKnightController controller;

    private void Start()
    {
        controller = GetComponent<FireKnightController>();
        anim = GetComponent<Animator>();

        // 1. Esconde a UI da mira por padrão
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (crosshairCanvasGroup != null) crosshairCanvasGroup.alpha = 0f;

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

            // Liga a UI, mas deixa invisível para o Fade fazer o trabalho
            if (crosshairUI != null) crosshairUI.SetActive(true);
            if (crosshairCanvasGroup != null) crosshairCanvasGroup.alpha = 0f; 
            if (chargeBarFill != null) chargeBarFill.fillAmount = 0f;
            
            if (camScript != null) camScript.SetAimingCamera(true);

            if (audioSource != null && currentWeapon?.swingSound != null)
                audioSource.PlayOneShot(currentWeapon.swingSound);
        }

        // 2. SEGURANDO O BOTÃO (Carregando)
        if (isChargingShot)
        {
            // Aumenta o tempo de carga
            currentChargeTime += Time.deltaTime;
            
            // Preenche a barra de progresso (de 0 a 1)
            if (chargeBarFill != null)
                chargeBarFill.fillAmount = Mathf.Clamp01(currentChargeTime / bowChargeDuration);

            // Faz a mira aparecer lentamente (Fade In)
            if (crosshairCanvasGroup != null)
                crosshairCanvasGroup.alpha = Mathf.Lerp(crosshairCanvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
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

        int damage = (currentWeapon != null ? currentWeapon.attackDamage : 10) + bonusDanoUpgrade;

        foreach (Collider enemy in hits)
        {
            Debug.Log($"[HIT] Inimigo atingido: {enemy.name} - Dano: {damage}");
            
            if (audioSource != null && currentWeapon?.hitSound != null)
                audioSource.PlayOneShot(currentWeapon.hitSound);

            if (hitSparks != null)
                Instantiate(hitSparks, enemy.ClosestPoint(transform.position), Quaternion.identity);

            if (enemy.TryGetComponent<EnemyDummy>(out EnemyDummy e))
                e.TakeDamage(damage);
        }
    }

    private void ShootProjectile()
    {
        if (rangedFirePoint == null || projectilePrefab == null) return;

        // 1. Cria um raio saindo do centro exato da tela da câmera
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        // 2. A MÁGICA AQUI: Diz para o Raycast acertar tudo, EXCETO a Layer "Player"
        // Certifique-se de que o nome da sua Layer está escrito exatamente assim (maiúsculas/minúsculas importam)
        int aimLayerMask = ~LayerMask.GetMask("player");

        // 3. Faz o Raycast passando a nossa aimLayerMask como filtro
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, aimLayerMask))
        {
            targetPoint = hit.point; // O alvo é a parede/chão/inimigo
            Debug.Log("<color=red>[MIRA] O raio da câmera bateu em: </color>" + hit.collider.name);
        }
        else
        {
            targetPoint = ray.GetPoint(100f); 
        }

        // 4. Calcula a direção exata
        Vector3 directionToTarget = targetPoint - rangedFirePoint.position;

        // 5. Instancia a flecha olhando para o alvo
        GameObject projectile = Instantiate(projectilePrefab, rangedFirePoint.position, Quaternion.LookRotation(directionToTarget));

        // Passa o dano para a flecha (AQUI É A MUDANÇA)
        ProjectileScript projScript = projectile.GetComponent<ProjectileScript>();
        if (projScript != null)
        {
            // Somamos o dano da arma ao bônus acumulado pelos upgrades
            projScript.damage = (currentWeapon != null ? currentWeapon.attackDamage : 10) + bonusDanoUpgrade;
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