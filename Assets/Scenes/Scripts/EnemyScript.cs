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
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private bool alreadyAttacked;
    public LayerMask whatIsPlayer;

    [Header("Feedback Visual")]
    public Renderer enemyRenderer;
    private Color originalColor;
    public float flashDuration = 0.1f;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        
        // Busca o player pela Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        
        if (enemyRenderer == null) enemyRenderer = GetComponent<Renderer>();
        originalColor = enemyRenderer.material.color;
    }

    void Update()
    {
        if (currentState == EnemyState.HitStun || currentState == EnemyState.Dead || player == null) return;

        bool playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInAttackRange) ChasePlayer();
        else AttackPlayer();
    }

    void ChasePlayer()
    {
        currentState = EnemyState.Chasing;
        agent.SetDestination(player.position);
    }

    [Header("Configurações de Ataque")]
    public int attackDamage = 10;
    public float dashForce = 5f;

    void AttackPlayer()
    {
        currentState = EnemyState.Attacking;
        agent.SetDestination(transform.position); 
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (!alreadyAttacked)
        {
            TentarDarDano();
            StartCoroutine(AttackSequence());
        }
    }

    IEnumerator AttackSequence()
    {
        alreadyAttacked = true;
        enemyRenderer.material.color = Color.yellow; 
        yield return new WaitForSeconds(0.4f); 

        TentarDarDano();

        Vector3 attackDir = (player.position - transform.position).normalized;
        float timer = 0;
        while(timer < 0.15f) 
        {
            transform.position += attackDir * dashForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        enemyRenderer.material.color = originalColor;
        yield return new WaitForSeconds(attackCooldown);
        alreadyAttacked = false; 
    }

    // FUNÇÃO DE SEGURANÇA: Procura o script de combate de forma profunda
    void TentarDarDano()
    {
        // Tenta no objeto tocado, depois no Pai, depois nos Filhos
        CombatScript playerCombat = player.GetComponent<CombatScript>();
        if (playerCombat == null) playerCombat = player.GetComponentInParent<CombatScript>();
        if (playerCombat == null) playerCombat = player.GetComponentInChildren<CombatScript>();

        if (playerCombat != null)
        {
            playerCombat.TakeDamage(attackDamage);
        }
        else
        {
            Debug.LogError($"ERRO: O Inimigo bateu em '{player.name}', mas não achou o CombatScript em lugar nenhum!");
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentState == EnemyState.Dead) return;
        currentHealth -= damage;
        StartCoroutine(FlashWhite());
        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashWhite()
    {
        EnemyState previousState = currentState;
        currentState = EnemyState.HitStun;
        agent.isStopped = true; 
        enemyRenderer.material.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        enemyRenderer.material.color = originalColor;
        agent.isStopped = false;
        if(currentState != EnemyState.Dead) currentState = previousState;
    }

    void Die()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
        Destroy(gameObject, 0.1f); 
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}