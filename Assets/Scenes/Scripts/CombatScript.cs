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

    void Update()
    {
        // Se o jogador não tem arma equipada, não faz nada
        if (currentWeapon == null) return;

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

    void OnDrawGizmosSelected()
    {
        // Usa o range da arma equipada para desenhar a esfera de debug
        if (attackPoint == null || currentWeapon == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);
    }
}