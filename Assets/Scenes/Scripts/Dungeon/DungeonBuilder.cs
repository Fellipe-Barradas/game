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
    [Tooltip("Prefab de tampa/parede para sockets que ficaram abertos. Deve conter um DoorSocket apontando para a face visível.")]
    public GameObject socketCapPrefab;
    [Tooltip("Prefab da porta colocada nos vãos que conectam duas salas. Deve conter um DoorSocket apontando para a frente da porta.")]
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
                GameObject cap = Instantiate(socketCapPrefab, s.WorldPosition, Quaternion.identity, root);
                SnapBySocket(cap, s.WorldPosition, s.WorldDirection);
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
            GameObject go = Instantiate(doorPrefab, dw.WorldPosition, Quaternion.identity, root);
            SnapBySocket(go, dw.WorldPosition, dw.WorldDirection);
            DoorController door = go.GetComponent<DoorController>();

            map.TryGetValue(dw.RoomA, out GameObject ra);
            map.TryGetValue(dw.RoomB, out GameObject rb);
            result.Add(new PlacedDoor { Door = door, RoomA = ra, RoomB = rb });
        }

        // DIAGNÓSTICO (temporário): verifica se algum tampão coincide com um vão de porta.
        int coincidencias = 0;
        foreach (PlannedDoorway dw in layout.Doorways)
            foreach (PlannedSocket os in layout.OpenSockets)
                if ((dw.WorldPosition - os.WorldPosition).sqrMagnitude < 0.25f) coincidencias++;
        Debug.Log($"[DungeonBuilder] salas={roomInstances.Count} portas={layout.Doorways.Count} " +
                  $"tampoes={layout.OpenSockets.Count} coincidencias_porta/tampao={coincidencias}");

        return result;
    }

    /// <summary>
    /// Encaixa o objeto recém-instanciado (rotação identidade) usando o DoorSocket interno do
    /// prefab como referência: rotaciona para o socket apontar em 'worldDir' e desloca para o
    /// socket coincidir com 'targetPos' (independe do pivô do prefab). Funciona em qualquer
    /// rotação de sala. Sem DoorSocket: posiciona em targetPos e cai no +Z (LookRotation).
    /// </summary>
    private static void SnapBySocket(GameObject instance, Vector3 targetPos, CardinalDirection worldDir)
    {
        Transform t = instance.transform;
        Vector3 target = CardinalDirections.ToVector(worldDir);
        target.y = 0f;

        DoorSocket socket = instance.GetComponentInChildren<DoorSocket>(true);
        if (socket == null)
        {
            t.position = targetPos;
            if (target.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(target.normalized);
            return;
        }

        // 1. Rotação: alinhar o apontador do socket à direção do vão (convenção do gizmo).
        if (target.sqrMagnitude > 0.0001f)
        {
            Vector3 socketFwd = socket.transform.parent != null
                ? socket.transform.parent.TransformDirection(CardinalDirections.ToVector(socket.direction))
                : CardinalDirections.ToVector(socket.direction);
            socketFwd.y = 0f;
            if (socketFwd.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.FromToRotation(socketFwd.normalized, target.normalized) * t.rotation;
        }

        // 2. Posição: deslocar para o DoorSocket interno coincidir com o socket da sala.
        //    (lê a posição do socket já rotacionado, depois corrige a translação)
        t.position += targetPos - socket.transform.position;
    }

    public void Clear()
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
