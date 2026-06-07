using UnityEngine;

/// <summary>Ponto de armadilha. Geralmente a armadilha já vem montada no prefab; este marker é opcional.</summary>
public class TrapMarker : MonoBehaviour
{
    public GameObject trapPrefabOverride;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}
