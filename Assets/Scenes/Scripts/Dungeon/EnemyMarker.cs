using UnityEngine;

/// <summary>Ponto de spawn de inimigo dentro do prefab da sala (não amarra prefab de inimigo).</summary>
public class EnemyMarker : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
