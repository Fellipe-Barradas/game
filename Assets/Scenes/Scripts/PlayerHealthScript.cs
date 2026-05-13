using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Status")]
    public int maxHealth = 100;
    public int currentHealth;

    private Renderer[] allRenderers;
    private List<Color> originalColors = new List<Color>();

    void Start()
    {
        currentHealth = maxHealth;

        allRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in allRenderers)
            originalColors.Add(r.material.HasProperty("_Color") ? r.material.color : Color.white);

        Invoke(nameof(SincronizarUI), 0.1f);
    }

    public void Curar(int quantidade) {
        currentHealth += quantidade;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        GameOverManager.Instance?.AtualizarVidaUI(currentHealth, maxHealth);
        Debug.Log("Vida atualizada: " + currentHealth);
    }

    void SincronizarUI()
    {
        GameOverManager.Instance?.AtualizarVidaUI(currentHealth, maxHealth);
    }

    public void ReceberDano(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        GameOverManager.Instance?.AtualizarVidaUI(currentHealth, maxHealth);

        StopAllCoroutines();
        StartCoroutine(FlashRed());

        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashRed()
    {
        foreach (Renderer r in allRenderers)
            if (r.material.HasProperty("_Color")) r.material.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < allRenderers.Length; i++)
            if (allRenderers[i].material.HasProperty("_Color"))
                allRenderers[i].material.color = originalColors[i];
    }

    void Die()
    {
        GameStateManager.Instance?.SetState(GameState.GameOver);
        if (TryGetComponent(out CombatScript combat)) combat.enabled = false;
    }
}