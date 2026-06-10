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
            // A direção do socket deve ser lida do MESMO jeito que o gizmo e o SnapBySocket:
            // a cardeal 'direction' interpretada no espaço do PARENT do socket (o holder de
            // parede pode estar rotacionado). Ler 's.direction' cru ignora essa rotação e faz
            // sockets laterais virarem Leste/Oeste — por isso o tampão/porta não girava neles.
            Vector3 worldDir = s.transform.parent != null
                ? s.transform.parent.TransformDirection(CardinalDirections.ToVector(s.direction))
                : CardinalDirections.ToVector(s.direction);
            CardinalDirection dir = NearestCardinal(transform.InverseTransformDirection(worldDir));

            sockets.Add(new RoomSocketData(
                transform.InverseTransformPoint(s.transform.position),
                dir));
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

    /// <summary>Cardeal mais próxima de um vetor no plano XZ (North=+Z, East=+X, South=-Z, West=-X).</summary>
    private static CardinalDirection NearestCardinal(Vector3 v)
    {
        v.y = 0f;
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.z))
            return v.x >= 0f ? CardinalDirection.East : CardinalDirection.West;
        return v.z >= 0f ? CardinalDirection.North : CardinalDirection.South;
    }
}
