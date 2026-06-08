using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

public struct PlacedDoor
{
    public DoorController Door;
    public GameObject RoomA;
    public GameObject RoomB;
}

/// <summary>Instancia o layout planejado e sela sockets abertos com prefab de parede.</summary>
public class DungeonBuilder : MonoBehaviour
{
    [Tooltip("Pai de todas as salas instanciadas (também alvo do NavMeshSurface).")]
    public Transform root;
    [Tooltip("Prefab de tampa/parede para sockets que ficaram abertos.")]
    public GameObject socketCapPrefab;
    [Tooltip("Prefab da porta colocada nos vãos que conectam duas salas.")]
    public GameObject doorPrefab;

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

    /// <summary>
    /// Instancia uma porta em cada vão de conexão. Deve ser chamado DEPOIS do bake de NavMesh
    /// e de os RoomControllers existirem. Retorna as portas com as duas salas que cada uma liga.
    /// </summary>
    public List<PlacedDoor> BuildDoors(DungeonLayout layout, List<GameObject> roomInstances)
    {
        var result = new List<PlacedDoor>();
        if (doorPrefab == null) return result;
        if (root == null) root = transform;

        // Mapa PlannedRoom -> instância (mesma ordem de layout.Rooms).
        var map = new Dictionary<PlannedRoom, GameObject>(layout.Rooms.Count);
        for (int i = 0; i < layout.Rooms.Count && i < roomInstances.Count; i++)
            map[layout.Rooms[i]] = roomInstances[i];

        foreach (PlannedDoorway dw in layout.Doorways)
        {
            Vector3 dir = CardinalDirections.ToVector(dw.WorldDirection);
            Quaternion rot = dir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(dir)
                : Quaternion.identity;

            GameObject go = Instantiate(doorPrefab, dw.WorldPosition, rot, root);
            DoorController door = go.GetComponent<DoorController>();

            map.TryGetValue(dw.RoomA, out GameObject ra);
            map.TryGetValue(dw.RoomB, out GameObject rb);
            result.Add(new PlacedDoor { Door = door, RoomA = ra, RoomB = rb });
        }
        return result;
    }

    public void Clear()
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
