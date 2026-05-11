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
        if (other.CompareTag("Player")) return;

        EnemyDummy enemy = other.GetComponent<EnemyDummy>();
        
        if (enemy == null) enemy = other.GetComponentInParent<EnemyDummy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}