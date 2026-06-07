using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Descrição pura de um candidato de sala (sem GameObject).
    /// O planner trabalha só com isto; PrefabRef é opaco (o builder converte de volta).
    /// </summary>
    public class RoomDefinition
    {
        public RoomType Type;
        public Bounds LocalBounds;
        public IReadOnlyList<RoomSocketData> Sockets;
        public float Weight = 1f;
        public object PrefabRef;
    }
}
