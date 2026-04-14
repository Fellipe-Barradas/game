using UnityEngine;

public class FireKnightCombat : MonoBehaviour
{
    [Header("Arma Equipada")]
    public WeaponData currentWeapon;

    [Header("Configuracao de Acerto")]
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Feedbacks Visuais e Referencias")]
    public Animator animator;
    public ParticleSystem hitSparks;
    public AudioSource audioSource;

    [Header("Saude do Jogador")]
    public int maxHealth = 100;

    [Header("Sons de Defesa e Dano")]
    public AudioClip blockSound;
    public AudioClip hurtSound;

    public bool isBlocking { get; private set; }

    private float nextAttackTime;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (currentWeapon == null)
        {
            return;
        }

        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerAct)
        {
            isBlocking = false;

            if (animator != null)
            {
                animator.SetBool("IsBlocking", false);
            }

            return;
        }

        if (Input.GetMouseButton(1))
        {
            isBlocking = true;

            if (animator != null)
            {
                animator.SetBool("IsBlocking", true);
            }
        }
        else
        {
            isBlocking = false;

            if (animator != null)
            {
                animator.SetBool("IsBlocking", false);
            }

            if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + 1f / currentWeapon.attackRate;
            }
        }
    }

    private void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (audioSource != null && currentWeapon.swingSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.swingSound);
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, currentWeapon.attackRange, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
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
                enemyScript.TakeDamage(currentWeapon.attackDamage);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isBlocking)
        {
            if (audioSource != null && blockSound != null)
            {
                audioSource.PlayOneShot(blockSound);
            }

            Debug.Log("Dano bloqueado.");
            return;
        }

        FireKnightController controller = GetComponent<FireKnightController>();
        if (controller != null && controller.isInvincible)
        {
            Debug.Log("Esquivou do ataque.");
            return;
        }

        currentHealth -= damage;

        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        Debug.Log("Jogador sofreu dano. Vida restante: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Jogador morreu.");

        isBlocking = false;

        if (animator != null)
        {
            animator.SetBool("IsBlocking", false);
        }

        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null)
        {
            stateManager.SetState(GameState.GameOver);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null || currentWeapon == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);
    }
}
