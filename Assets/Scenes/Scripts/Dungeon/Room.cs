using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>
/// Componente da raiz do prefab de sala. Converte a autoria visual em RoomDefinition pura.
/// Assume o BoxCollider de bounds na própria raiz (escala 1).
/// </summary>
public class Room : MonoBehaviour
{
    public RoomType type;
    [Tooltip("BoxCollider (isTrigger) cobrindo a sala, na raiz. Usado como bounds e como gatilho de entrada.")]
    public BoxCollider boundsCollider;

    public RoomDefinition BuildDefinition(object prefabRef, float weight)
    {
        var sockets = new List<RoomSocketData>();
        foreach (DoorSocket s in GetComponentsInChildren<DoorSocket>(true))
        {
            sockets.Add(new RoomSocketData(
                transform.InverseTransformPoint(s.transform.position),
                s.direction));
        }

        Bounds local = boundsCollider != null
            ? new Bounds(boundsCollider.center, boundsCollider.size)
            : new Bounds(Vector3.zero, new Vector3(10, 4, 10));

        return new RoomDefinition
        {
            Type = type,
            LocalBounds = local,
            Sockets = sockets,
            Weight = weight,
            PrefabRef = prefabRef
        };
    }
}
