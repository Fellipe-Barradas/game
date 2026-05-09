using UnityEngine;
using UnityEngine.InputSystem;

public class FireKnightCombat : MonoBehaviour
{
    [Header("Arma Equipada")]
    public WeaponData currentWeapon;

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

    [Header("Saúde do Jogador")]
    public int maxHealth = 100;

    [Header("Sons")]
    public AudioSource audioSource;
    public AudioClip blockSound;
    public AudioClip hurtSound;

    public bool isBlocking { get; private set; }

    private float nextAttackTime;
    private int currentHealth;
    private bool isChargingShot = false;
    private FireKnightController controller;

    private void Start()
    {
        currentHealth = maxHealth;
        controller    = GetComponent<FireKnightController>();
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

    // A checagem do cooldown vem ANTES de qualquer coisa
    if (!isBlocking && mouse.leftButton.wasPressedThisFrame)
    {
        if (Time.time >= nextAttackTime)
        {
            float rate     = currentWeapon != null ? currentWeapon.attackRate : 1f;
            nextAttackTime = Time.time + 1f / rate;
            Attack();
        }
        // Se ainda no cooldown, não faz nada — nem som
    }
}

    private void HandleArcherInput(Mouse mouse)
{
    // Segurou — apenas entra em mira, sem trigger de animação
    if (mouse.leftButton.wasPressedThisFrame && !isChargingShot)
    {
        isChargingShot = true;
        controller.SetAiming(true);

        if (audioSource != null && currentWeapon?.swingSound != null)
            audioSource.PlayOneShot(currentWeapon.swingSound);
    }

    // Soltou — sai da mira e dispara
    if (mouse.leftButton.wasReleasedThisFrame && isChargingShot)
    {
        isChargingShot = false;
        controller.SetAiming(false);
        
        // Calcula o tempo de duração da animação (cooldown)
        float rate = currentWeapon != null ? currentWeapon.attackRate : 1f;
        float animDuration = 1f / rate; // Exemplo: se attackRate é 1, dura 2 segundos. Se é 2, dura 1 segundo.
        
        // Envia esse tempo para o Controller. 
        // O Rig agora ficará ativo exatamente até a animação terminar!
        controller.TriggerAttackAnimation();

        nextAttackTime = Time.time + animDuration;
    }

    // Botão direito cancela sem disparar
    if (mouse.rightButton.wasPressedThisFrame && isChargingShot)
        CancelAim();
}

    private void CancelAim()
    {
        if (!isChargingShot) return;
        isChargingShot = false;
        controller.SetAiming(false);
    }

    private void Attack()
    {
        if (controller != null)
            controller.TriggerAttackAnimation();

        if (audioSource != null && currentWeapon?.swingSound != null)
            audioSource.PlayOneShot(currentWeapon.swingSound);
    }

    /// <summary>
    /// MÉTODO PARA OS ANIMADORES:
    /// Adicione um Animation Event no frame do impacto e chame "ExecuteAttackEvent".
    /// </summary>
    public void ExecuteAttackEvent()
    {
        if (controller == null) return;

        if (controller.currentClass == PlayerClass.Arqueiro)
            ShootProjectile();
        else
            PerformMeleeAttack();
    }

    private void PerformMeleeAttack()
    {
        if (meleeAttackPoint == null) return;

        Vector3 hitboxSize = controller.currentClass == PlayerClass.Espadachim
            ? new Vector3(swordAttackWidth, 2f, swordAttackRange)
            : new Vector3(lanceAttackWidth, 2f, lanceAttackRange);

        Vector3 boxCenter = meleeAttackPoint.position
                          + meleeAttackPoint.forward * (hitboxSize.z / 2f);

        Collider[] hits = Physics.OverlapBox(
            boxCenter, hitboxSize / 2f, meleeAttackPoint.rotation, enemyLayers);

        int damage = currentWeapon != null ? currentWeapon.attackDamage : 10;

        foreach (Collider enemy in hits)
        {
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
        Instantiate(projectilePrefab, rangedFirePoint.position, rangedFirePoint.rotation);
    }

    public void TakeDamage(int damage)
    {
        if (isBlocking)
        {
            if (audioSource != null && blockSound != null)
                audioSource.PlayOneShot(blockSound);
            Debug.Log("Dano bloqueado.");
            return;
        }

        if (controller != null && controller.isInvincible)
        {
            Debug.Log("Esquivou do ataque.");
            return;
        }

        currentHealth -= damage;

        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);

        Debug.Log("Jogador sofreu dano. Vida restante: " + currentHealth);

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("Jogador morreu.");
        isBlocking = false;
        GameStateManager.Instance?.SetState(GameState.GameOver);
    }

    /// <summary>
    /// MÉTODO PARA OS DESIGNERS:
    /// Desenha os hitboxes no Scene View para balanceamento.
    /// </summary>
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

        Vector3 boxCenter = meleeAttackPoint.position
                          + meleeAttackPoint.forward * (hitboxSize.z / 2f);

        Gizmos.matrix = Matrix4x4.TRS(boxCenter, meleeAttackPoint.rotation, hitboxSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        if (currentWeapon != null)
        {
            Gizmos.color  = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(meleeAttackPoint.position, currentWeapon.attackRange);
        }
    }
}