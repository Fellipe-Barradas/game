using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    public float speed = 20f;
    public float lifeTime = 5f;
    
    [HideInInspector]
    public int damage;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[PROJECTILE] Colidiu com: " + other.name);
        if (other.CompareTag("Player")) return;

        EnemyDummy enemy = other.GetComponent<EnemyDummy>();
        
        if (enemy == null) enemy = other.GetComponentInParent<EnemyDummy>();

        if (enemy != null)
        {
            Debug.Log($"[PROJECTILE] Acertou inimigo: {enemy.name} - Dano: {damage}");
            enemy.TakeDamage(damage);
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}