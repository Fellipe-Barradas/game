using System.Collections;
using UnityEngine;

public class EnemyDummy : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Feedback Visual")]
    public Renderer enemyRenderer;
    private Color originalColor;
    public float flashDuration = 0.1f; // Milissegundos piscando em branco (GDD)

    void Start()
    {
        currentHealth = maxHealth;
        
        if (enemyRenderer == null)
            enemyRenderer = GetComponent<Renderer>();
            
        originalColor = enemyRenderer.material.color;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Ativa o Hit Stun (Piscar em branco) - Conforme GDD
        StartCoroutine(FlashWhite());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashWhite()
    {
        // Muda a cor para branco
        enemyRenderer.material.color = Color.white;
        
        // Espera os milissegundos definidos
        yield return new WaitForSeconds(flashDuration);
        
        // Volta para a cor original
        enemyRenderer.material.color = originalColor;
    }

    void Die()
    {
        Debug.Log("Inimigo Derrotado! Coleta de Fragmento iniciada."); // Loop do GDD
        Destroy(gameObject);
    }
}