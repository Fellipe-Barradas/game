using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDummy : MonoBehaviour
{
    [Header("Status do Inimigo")]
    public int maxHealth = 100;
    private int currentHealth;

    public enum EnemyState { Chasing, Attacking, HitStun, Dead }
    public EnemyState currentState;

    [Header("Navegação e IA")]
    public NavMeshAgent agent;
    private Transform player;
    public float attackRange   = 2f;
    public float attackCooldown = 1.5f;
    private bool alreadyAttacked;
    public LayerMask whatIsPlayer;

    [Header("Configurações de Ataque")]
    public int attackDamage = 10;
    public float dashForce  = 5f;

    [Header("Feedback Visual")]
    public Renderer enemyRenderer;
    private Color originalColor;
    public float flashDuration = 0.1f;

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
        // 1. Atualiza a animação de movimento baseado na velocidade REAL do agente
        // Se a velocidade for maior que 0.1, ele está se movendo.
        if (anim != null && agent != null)
        {
            bool isActuallyMoving = agent.velocity.magnitude > 0.1f;
            anim.SetBool(HashIsMoving, isActuallyMoving);
        }

        if (currentState == EnemyState.HitStun ||
            currentState == EnemyState.Dead    ||
            player == null) return;

        bool playerInAttackRange = Physics.CheckSphere(
            transform.position, attackRange, whatIsPlayer);

        if (!playerInAttackRange) ChasePlayer();
        else                      AttackPlayer();
    }

    void ChasePlayer()
    {
        currentState = EnemyState.Chasing;
        agent.isStopped = false; // Garante que ele pode andar
        agent.SetDestination(player.position);
    }

    void AttackPlayer()
    {
        currentState = EnemyState.Attacking;
        
        // Para o agente completamente para que a velocidade zere e a animação pare
        agent.isStopped = true;
        agent.SetDestination(transform.position); 
        
        transform.LookAt(new Vector3(
            player.position.x, transform.position.y, player.position.z));

        if (!alreadyAttacked)
            StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        alreadyAttacked = true;
        anim.SetTrigger(HashAttack); 

        yield return new WaitForSeconds(0.4f);
        TentarDarDano();

        Vector3 attackDir = (player.position - transform.position).normalized;
        float timer = 0f;
        while (timer < 0.15f)
        {
            transform.position += attackDir * dashForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(attackCooldown);
        alreadyAttacked = false;
        
        // Permite que ele volte a andar caso o player saia do alcance
        if (currentState != EnemyState.Dead && currentState != EnemyState.HitStun)
        {
             agent.isStopped = false;
        }
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
        StartCoroutine(FlashWhite());

        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashWhite()
    {
        EnemyState previousState = currentState;
        currentState     = EnemyState.HitStun;
        agent.isStopped  = true; // A velocidade vai a 0, parando a animação de walk automaticamente

        if (enemyRenderer != null)
            enemyRenderer.material.color = Color.white;

        yield return new WaitForSeconds(flashDuration);

        if (enemyRenderer != null)
            enemyRenderer.material.color = originalColor;

        if (currentState != EnemyState.Dead)
        {
            currentState = previousState;
            agent.isStopped = false; // Volta a se mover
        }
    }

    void Die()
    {
        currentState    = EnemyState.Dead;
        agent.isStopped = true;
        anim.SetTrigger(HashIsDead);
        Destroy(gameObject, 2f); 
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}