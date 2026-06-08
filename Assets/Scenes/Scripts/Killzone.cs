using UnityEngine;

public class Killzone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Garante que só o colisor principal do Player ativa a morte
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            if (other.TryGetComponent(out CombatScript combat))
            {
                // Aplica o dano massivo para zerar a vida direto
                combat.TakeDamage(9999); 
                Debug.Log("[KILLZONE] Player caiu no vão e morreu.");
            }
        }
    }
}