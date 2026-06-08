using UnityEngine;

public class ChestMarker : MonoBehaviour
{
    [Range(0f, 100f)]
    [Tooltip("Chance desta posição virar um baú no spawn.")]
    public float spawnChance = 100f;

    [Tooltip("Opcional: força um baú específico aqui (ignora a ChestTable).")]
    public ChestSO chestOverride;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}
