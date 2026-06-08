using UnityEngine;
using Game.Dungeon;

/// <summary>Marker de porta no prefab de sala. O forward visual segue a direção cardeal local.</summary>
public class DoorSocket : MonoBehaviour
{
    public CardinalDirection direction = CardinalDirection.North;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 p = transform.position;
        Vector3 dir = transform.parent != null
            ? transform.parent.TransformDirection(CardinalDirections.ToVector(direction))
            : CardinalDirections.ToVector(direction);
        Gizmos.DrawSphere(p, 0.25f);
        Gizmos.DrawLine(p, p + dir.normalized * 1.5f);
    }
}
