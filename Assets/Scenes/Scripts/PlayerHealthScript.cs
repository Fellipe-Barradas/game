using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necessário para usar Listas

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Feedback de Dano")]
    // Mudamos para uma lista para pegar todas as partes do modelo
    private Renderer[] allRenderers; 
    private List<Color> originalColors = new List<Color>();

    void Start()
    {
        Debug.Log(">>> O SCRIPT DE VIDA FOI INICIADO NO PLAYER! <<<");
        currentHealth = maxHealth;

        // Busca automaticamente todos os renderers nos objetos filhos (Bags, Boots, etc)
        allRenderers = GetComponentsInChildren<Renderer>();

        // Salva a cor original de cada parte
        foreach (Renderer r in allRenderers)
        {
            originalColors.Add(r.material.color);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Vida do Player: " + currentHealth);

        StopAllCoroutines(); // Interrompe uma piscada anterior se houver
        StartCoroutine(FlashRed());

        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashRed()
    {
        // Faz todas as partes ficarem vermelhas ao mesmo tempo
        foreach (Renderer r in allRenderers)
        {
            r.material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Devolve a cor original para cada parte
        for (int i = 0; i < allRenderers.Length; i++)
        {
            allRenderers[i].material.color = originalColors[i];
        }
    }

    void Die()
    {
        Debug.Log("Game Over!");
        gameObject.SetActive(false);
    }
}