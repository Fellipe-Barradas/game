using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossEnemy : MonoBehaviour, IDamageable
{
    [Header("Status")]
    public int maxHealth = 500;
    private int currentHealth;

    public enum BossState { Idle, Chasing, Attacking, HitStun, Dead }
    public BossState currentState;

    [Header("Navegação")]
    public NavMeshAgent agent;
    private Transform player;
    public float attackRange    = 3f;
    public float attackCooldown = 2f;
    private bool alreadyAttacked;
    public LayerMask whatIsPlayer;

    [Header("Ataque")]
    public int attackDamage = 40;
    public float dashForce  = 3f;
    [Tooltip("Tempo (s) do início da animação até o golpe conectar. Ajuste para casar com o momento do impacto na animação.")]
    public float attackWindup = 0.6f;

    private Coroutine attackRoutine;

    [Header("Hit")]
    public float hitStunDuration = 0.3f;

    [Header("Detecção")]
    public float detectionRange = 15f;
    public float chaseRange     = 20f;
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

        if (anim == null)
            Debug.LogError("Animator não encontrado nos filhos de " + gameObject.name);
    }

    void Update()
    {
        if (anim != null && agent != null)
        {
            bool isActuallyMoving = agent.velocity.magnitude > 0.1f;
            anim.SetBool(HashIsMoving, isActuallyMoving);
        }

        if (currentState == BossState.HitStun ||
            currentState == BossState.Dead    ||
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

        bool playerInRange = Physics.CheckSphere(
            transform.position, attackRange, whatIsPlayer);

        if (!playerInRange) ChasePlayer();
        else                AttackPlayer();
    }

    void ReturnToIdle()
    {
        currentState    = BossState.Idle;
        agent.isStopped = true;
        agent.SetDestination(transform.position);
    }

    void ChasePlayer()
    {
        currentState    = BossState.Chasing;
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void AttackPlayer()
    {
        currentState    = BossState.Attacking;
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
        // — assim, acertar o boss durante o stun cancela o dano dele.
        if (currentState == BossState.Attacking)
        {
            TentarDarDano();

            Vector3 dir = (player.position - transform.position).normalized;
            float timer = 0f;
            while (timer < 0.2f)
            {
                transform.position += dir * dashForce * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        alreadyAttacked = false;
        attackRoutine   = null;

        if (currentState != BossState.Dead && currentState != BossState.HitStun)
            agent.isStopped = false;
    }

    // Cancela o ataque em andamento: ao tomar dano, o golpe do boss é interrompido
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
        if (currentState == BossState.Dead) return;

        currentHealth -= damage;
        anim.SetTrigger(HashHitTrigger);
        InterromperAtaque();
        StartCoroutine(HitStun());

        if (currentHealth <= 0) Die();
    }

    IEnumerator HitStun()
    {
        BossState previousState = currentState;
        currentState    = BossState.HitStun;
        agent.isStopped = true;

        yield return new WaitForSeconds(hitStunDuration);

        agent.isStopped = false;
        if (currentState != BossState.Dead)
            currentState = previousState;
    }

    void Die()
    {
        currentState    = BossState.Dead;
        agent.isStopped = true;
        anim.SetTrigger(HashIsDead);
        Destroy(gameObject, 3f);
        GameStateManager.Instance?.TriggerVictory();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}