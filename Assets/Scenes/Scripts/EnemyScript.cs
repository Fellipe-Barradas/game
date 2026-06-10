using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDummy : MonoBehaviour, IDamageable
{
    [Header("Status do Inimigo")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Drop de Moedas")]
    public int dropPrata      = 0;
    public int dropOuro       = 0;
    public int dropFragmentos = 0;

    public enum EnemyState { Idle, Chasing, Attacking, HitStun, Dead }
    public EnemyState currentState;

    [Header("Navegação e IA")]
    public NavMeshAgent agent;
    private Transform player;
    public float attackRange    = 2f;
    public float attackCooldown = 1.5f;
    private bool alreadyAttacked;
    public LayerMask whatIsPlayer;

    [Header("Configurações de Ataque")]
    public int attackDamage = 10;
    public float dashForce  = 5f;
    [Tooltip("Tempo (s) do início da animação até o golpe conectar. Ajuste para casar com o momento do impacto na animação.")]
    public float attackWindup = 0.75f;

    private Coroutine attackRoutine;

    [Header("Feedback Visual")]
    public Renderer enemyRenderer;
    private Color originalColor;
    public float flashDuration = 0.1f;

    [Header("Detecção")]
    public float detectionRange = 10f;
    public float chaseRange     = 15f;
    private bool isAggro        = false;

    // Animação
    private Animator anim;
    private static readonly int HashIsMoving   = Animator.StringToHash("isMoving");
    private static readonly int HashAttack     = Animator.StringToHash("attack");
    private static readonly int HashHitTrigger = Animator.StringToHash("hitTrigger");
    private static readonly int HashIsDead     = Animator.StringToHash("isDead");

    void Start()
    {
        currentHealth = maxHealth;
        agent         = GetComponent<NavMeshAgent>();
        anim          = GetComponentInChildren<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();

        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;
    }

    void Update()
    {
        if (anim != null && agent != null)
        {
            bool isActuallyMoving = agent.velocity.magnitude > 0.1f;
            anim.SetBool(HashIsMoving, isActuallyMoving);
        }

        if (currentState == EnemyState.HitStun ||
            currentState == EnemyState.Dead    ||
            player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Detecta o jogador
        if (!isAggro && distanceToPlayer <= detectionRange)
            isAggro = true;

        // Desiste se o jogador for longe demais
        if (isAggro && distanceToPlayer > chaseRange)
        {
            isAggro = false;
            ReturnToIdle();
            return;
        }

        // Só age se estiver em aggro
        if (!isAggro) return;

        bool playerInAttackRange = Physics.CheckSphere(
            transform.position, attackRange, whatIsPlayer);

        if (!playerInAttackRange) ChasePlayer();
        else                      AttackPlayer();
    }

    void ReturnToIdle()
    {
        currentState    = EnemyState.Idle;
        agent.isStopped = true;
        agent.SetDestination(transform.position);
    }

    void ChasePlayer()
    {
        currentState    = EnemyState.Chasing;
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void AttackPlayer()
    {
        currentState    = EnemyState.Attacking;
        agent.isStopped = true;
        agent.SetDestination(transform.position);

        transform.LookAt(new Vector3(
            player.position.x, transform.position.y, player.position.z));

        if (!alreadyAttacked)
            attackRoutine = StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        alreadyAttacked = true;
        anim.SetTrigger(HashAttack);

        yield return new WaitForSeconds(attackWindup);

        // Só conecta o golpe se o ataque NÃO foi interrompido (por hit/morte)
        // e o jogador continua no alcance — assim, bater no inimigo cancela o dano dele.
        if (currentState == EnemyState.Attacking)
        {
            TentarDarDano();

            Vector3 attackDir = (player.position - transform.position).normalized;
            float timer = 0f;
            while (timer < 0.15f)
            {
                transform.position += attackDir * dashForce * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        alreadyAttacked = false;
        attackRoutine   = null;

        if (currentState != EnemyState.Dead && currentState != EnemyState.HitStun)
            agent.isStopped = false;
    }

    // Cancela o ataque em andamento: ao tomar dano, o golpe do inimigo é interrompido
    void InterromperAtaque()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        alreadyAttacked = false;
        if (anim != null) anim.ResetTrigger(HashAttack);
    }

    void TentarDarDano()
    {
        CombatScript playerCombat = player.GetComponent<CombatScript>();
        if (playerCombat == null) playerCombat = player.GetComponentInParent<CombatScript>();
        if (playerCombat == null) playerCombat = player.GetComponentInChildren<CombatScript>();

        if (playerCombat != null)
            playerCombat.TakeDamage(attackDamage);
        else
            Debug.LogError($"ERRO: '{player.name}' não tem CombatScript!");
    }

    public void TakeDamage(int damage)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        anim.SetTrigger(HashHitTrigger);
        InterromperAtaque();
        StartCoroutine(FlashWhite());

        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashWhite()
    {
        EnemyState previousState = currentState;
        currentState    = EnemyState.HitStun;
        agent.isStopped = true;

        if (enemyRenderer != null)
            enemyRenderer.material.color = Color.white;

        yield return new WaitForSeconds(flashDuration);

        if (enemyRenderer != null)
            enemyRenderer.material.color = originalColor;

        if (currentState != EnemyState.Dead)
        {
            currentState    = previousState;
            agent.isStopped = false;
        }
    }

    void Die()
    {
        currentState    = EnemyState.Dead;
        agent.isStopped = true;
        anim.SetTrigger(HashIsDead);

        GerenciadorMoedas.Instancia?.AdicionarDrops(dropPrata, dropOuro, dropFragmentos);

        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}