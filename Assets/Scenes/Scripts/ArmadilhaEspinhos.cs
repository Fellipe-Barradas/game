using UnityEngine;

public class ArmadilhaEspinhos : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Ignora colisores trigger (ex.: gatilhos de interação do próprio player).
        if (other.isTrigger) return;

        // Acha o player por componente, não pela tag "Player" (que está em outro
        // objeto da hierarquia). Mesmo caminho de morte instantânea da Killzone.
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.Morrer();
            Debug.Log("[ARMADILHA] O jogador caiu nos espinhos e morreu instantaneamente!", this);
        }
    }
}
