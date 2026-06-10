using UnityEngine;

public class Killzone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Ignora colisores trigger (ex.: gatilhos de interação do próprio player).
        if (other.isTrigger) return;

        // Acha o player por componente, não pela tag "Player" (que está em outro
        // objeto da hierarquia). Funciona com o capsule ou qualquer filho.
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.Morrer();
            Debug.Log("[KILLZONE] Player caiu no vão e morreu.", this);
        }
    }
}
