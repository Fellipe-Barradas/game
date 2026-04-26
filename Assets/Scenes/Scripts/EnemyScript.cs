using System.Collections;
using UnityEngine;
using UnityEngine.AI; // Necessário para a navegação

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
    public float sightRange = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private bool alreadyAttacked;
    public LayerMask whatIsPlayer;

    [Header("Feedback Visual (GDD)")]
    public Renderer enemyRenderer;
    private Color originalColor;
    public float flashDuration = 0.1f;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        if (enemyRenderer == null)
            enemyRenderer = GetComponent<Renderer>();
            
        originalColor = enemyRenderer.material.color;
    }

    void Update()
    {
        // Se estiver em HitStun ou morto, não executa lógica de movimento
        if (currentState == EnemyState.HitStun || currentState == EnemyState.Dead) return;

        bool playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInAttackRange)
        {
            ChasePlayer();
        }
        else
        {
            AttackPlayer();
        }
        Debug.DrawLine(transform.position, player.position, Color.red);
    }

    void ChasePlayer()
    {
        currentState = EnemyState.Chasing;
        agent.SetDestination(player.position);
    }

    [Header("Configurações de Ataque")]
    public int attackDamage = 10;
    public float dashForce = 5f; // Força do pulo/investida no player

    // Esta função decide QUE o inimigo vai atacar
    void AttackPlayer()
    {
        currentState = EnemyState.Attacking;
        
        // Faz o inimigo parar de deslizar pelo NavMesh enquanto ataca
        agent.SetDestination(transform.position); 

        // Faz ele olhar para o player (sem inclinar o corpo)
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // Se ele não estiver em "cooldown", ele inicia a sequência de ataque
        if (!alreadyAttacked)
        {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        else
        {
            Debug.LogError("ERRO: O Inimigo te tocou, mas não achou o script PlayerHealth no seu Player!");

        }
            StartCoroutine(AttackSequence());
        }
    }

    // Esta função controla COMO o ataque acontece (o tempo, a cor, o dano)
    IEnumerator AttackSequence()
    {
        alreadyAttacked = true;
        
        // 1. AVISO (Telegraphing): Inimigo fica amarelo avisando que vai bater
        enemyRenderer.material.color = Color.yellow; 
        yield return new WaitForSeconds(0.4f); // Tempo de reação para o jogador

        // 2. O DANO: Procura o componente de vida no player e aplica o dano
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        // 3. MOVIMENTO: Dá um pequeno pulo/investida na direção do player
        Vector3 attackDir = (player.position - transform.position).normalized;
        float timer = 0;
        while(timer < 0.15f) 
        {
            transform.position += attackDir * dashForce * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null; // Espera o próximo frame (suaviza o movimento)
        }

        // 4. DESCANSO: Volta à cor original e espera o cooldown
        enemyRenderer.material.color = originalColor;
        yield return new WaitForSeconds(attackCooldown);
        
        alreadyAttacked = false; // Pronto para atacar de novo
    }

    public void TakeDamage(int damage)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        StartCoroutine(FlashWhite());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashWhite()
    {
        EnemyState previousState = currentState;
        currentState = EnemyState.HitStun;
        
        // No HitStun o inimigo para de se mover por um breve momento
        agent.isStopped = true; 
        enemyRenderer.material.color = Color.white;
        
        yield return new WaitForSeconds(flashDuration);
        
        enemyRenderer.material.color = originalColor;
        agent.isStopped = false;
        
        if(currentState != EnemyState.Dead)
            currentState = previousState;
    }

    void Die()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
        Debug.Log("Inimigo Derrotado! Coleta de Fragmento iniciada."); 
        
        // Aqui futuramente chamaremos o script de Loot/Fragmento
        Destroy(gameObject, 0.1f); 
    }

    // Visualização dos alcances no Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}