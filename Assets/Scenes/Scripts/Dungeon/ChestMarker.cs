using UnityEngine;

public class ChestMarker : MonoBehaviour
{
    [Tooltip("Opcional: prefab de baú específico. Vazio = usa o padrão do RoomPopulator.")]
    public GameObject chestPrefabOverride;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}
