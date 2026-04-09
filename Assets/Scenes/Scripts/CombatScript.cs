using UnityEngine;

public class FireKnightCombat : MonoBehaviour
{
    [Header("Arma Equipada")]
    public WeaponData currentWeapon; // O script agora puxa TUDO daqui!

    [Header("Configuração de Acerto")]
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Feedbacks Visuais e Referências")]
    public Animator animator; 
    public ParticleSystem hitSparks; 
    public AudioSource audioSource;

    private float nextAttackTime = 0f;
    public bool isBlocking { get; private set; } = false;

    [Header("Saúde do Jogador")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Sons de Defesa e Dano")]
    public AudioClip blockSound; // Som de espadas batendo ou escudo
    public AudioClip hurtSound;  // Som do jogador tomando dano

    void Update()
    {
        // Se o jogador não tem arma equipada, não faz nada
        if (currentWeapon == null) return;

        currentHealth = maxHealth;

        // DEFESA
        if (Input.GetMouseButton(1))
        {
            isBlocking = true;
            if (animator != null) animator.SetBool("IsBlocking", true);
        }
        else
        {
            isBlocking = false;
            if (animator != null) animator.SetBool("IsBlocking", false);

            // ATAQUE
            if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + 1f / currentWeapon.attackRate;
            }
        }
    }

    void Attack()
    {
        // 1. Animação e Som Específico da Arma
        if (animator != null) animator.SetTrigger("Attack");
        if (audioSource != null && currentWeapon.swingSound != null) 
        {
            audioSource.PlayOneShot(currentWeapon.swingSound);
        }

        // 2. Detectar inimigos usando o alcance específico da Arma
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, currentWeapon.attackRange, enemyLayers);

        // 3. Aplicar Dano e Feedbacks
        foreach (Collider enemy in hitEnemies)
        {
            // Toca o som de impacto específico da arma
            if (audioSource != null && currentWeapon.hitSound != null) 
            {
                audioSource.PlayOneShot(currentWeapon.hitSound);
            }

            if (hitSparks != null)
            {
                Instantiate(hitSparks, enemy.ClosestPoint(transform.position), Quaternion.identity);
            }

            EnemyDummy enemyScript = enemy.GetComponent<EnemyDummy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(currentWeapon.attackDamage); // Causa o dano da arma
            }
        }
    }

    public void TakeDamage(int damage)
    {
        // 1. Verifica se a Defesa está ativa
        if (isBlocking)
        {
            // Feedback de Bloqueio (Som) e anula o dano
            if (audioSource != null && blockSound != null)
            {
                audioSource.PlayOneShot(blockSound);
            }
            Debug.Log("Dano Bloqueado!");
            return; // Interrompe o código aqui para não tomar dano
        }

        // 2. Verifica se está no meio de um Dash (Invencibilidade do GDD)
        FireKnightController controller = GetComponent<FireKnightController>();
        if (controller != null && controller.isInvincible)
        {
            Debug.Log("Esquivou do ataque! (I-frames)");
            return; 
        }

        // 3. Se não defendeu nem esquivou, toma dano
        currentHealth -= damage;
        
        // Feedback Sonoro de Dano
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        Debug.Log("Jogador sofreu dano! Vida restante: " + currentHealth);

        // 4. Condição de Derrota (GDD)
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Jogador Morreu! Voltando ao Menu Principal e perdendo os itens...");
        // TODO: Lógica de recarregar a cena do Menu Principal
    }

    void OnDrawGizmosSelected()
    {
        // Usa o range da arma equipada para desenhar a esfera de debug
        if (attackPoint == null || currentWeapon == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);
    }
}