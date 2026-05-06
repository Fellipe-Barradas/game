using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    public float speed = 20f;
    public float lifeTime = 5f;
    public int damage = 10;
    
    [Header("Efeitos Visuais")]
    public GameObject impactEffectPrefab; // Partícula ao atingir algo (Opcional)

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Dispara o projétil para a frente
        rb.linearVelocity = transform.forward * speed;

        // Destrói o projétil após um tempo para não pesar a memória
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignora colisão com o próprio jogador
        if (other.CompareTag("Player")) return;

        Debug.Log("Projétil atingiu: " + other.name);

        // Se o objeto tiver script de vida, cause dano
        // if (other.TryGetComponent<EnemyHealth>(out EnemyHealth enemy)) {
        //     enemy.TakeDamage(damage);
        // }

        // Feedback visual
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destrói o projétil ao bater em algo
        Destroy(gameObject);
    }
}