using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>Instancia o layout planejado e sela sockets abertos com prefab de parede.</summary>
public class DungeonBuilder : MonoBehaviour
{
    [Tooltip("Pai de todas as salas instanciadas (também alvo do NavMeshSurface).")]
    public Transform root;
    [Tooltip("Prefab de tampa/parede para sockets que ficaram abertos.")]
    public GameObject socketCapPrefab;

    /// <summary>Instancia o layout. Retorna a sala raiz de cada PlannedRoom na mesma ordem de layout.Rooms.</summary>
    public List<GameObject> Build(DungeonLayout layout)
    {
        if (root == null) root = transform;
        var instances = new List<GameObject>(layout.Rooms.Count);

        foreach (PlannedRoom pr in layout.Rooms)
        {
            var prefab = (GameObject)pr.Definition.PrefabRef;
            GameObject go = Instantiate(prefab, pr.Position, Quaternion.Euler(0, pr.Yaw, 0), root);
            instances.Add(go);
        }

        if (socketCapPrefab != null)
        {
            foreach (PlannedSocket s in layout.OpenSockets)
            {
                Vector3 dir = CardinalDirections.ToVector(s.WorldDirection);
                Quaternion rot = dir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(dir)
                    : Quaternion.identity;
                Instantiate(socketCapPrefab, s.WorldPosition, rot, root);
            }
        }

        return instances;
    }

    public void Clear()
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
