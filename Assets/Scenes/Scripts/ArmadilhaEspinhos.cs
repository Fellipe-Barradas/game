using UnityEngine;

public class ArmadilhaEspinhos : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Verifica se quem caiu foi o jogador principal
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            // Busca o CombatScript para aplicar o dano fatal
            if (other.TryGetComponent(out CombatScript combat))
            {
                // Passamos um valor absurdamente alto (ex: 9999) para garantir a morte instantânea,
                // ignorando até mesmo se o jogador estiver defendendo
                combat.TakeDamage(9999); 
                Debug.Log("[ARMADILHA] O jogador caiu nos espinhos e morreu instantaneamente!");
            }
        }
    }
}