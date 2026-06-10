using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

[CreateAssetMenu(menuName = "Dungeon/Room Catalog", fileName = "RoomCatalog")]
public class RoomCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        public float weight = 1f;
    }

    public List<Entry> rooms = new List<Entry>();

    /// <summary>Lê os componentes Room dos prefabs e produz as definições puras para o planner.</summary>
    public List<RoomDefinition> BuildDefinitions()
    {
        var defs = new List<RoomDefinition>();
        foreach (Entry e in rooms)
        {
            if (e.prefab == null) continue;
            Room room = e.prefab.GetComponent<Room>();
            if (room == null)
            {
                Debug.LogWarning($"[RoomCatalog] Prefab {e.prefab.name} sem componente Room — ignorado.");
                continue;
            }
            defs.Add(room.BuildDefinition(e.prefab, e.weight));
        }
        return defs;
    }
}
