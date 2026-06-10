using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    public class PlannedSocket
    {
        public Vector3 WorldPosition;
        public CardinalDirection WorldDirection;
        public bool Used;
    }

    public class PlannedRoom
    {
        public RoomDefinition Definition;
        public int Yaw;                 // 0/90/180/270
        public Vector3 Position;        // translação em mundo
        public int Depth;               // distância no grafo a partir do início
        public Bounds WorldBounds;
        public List<PlannedSocket> Sockets = new List<PlannedSocket>();
    }

    public class PlannedDoorway
    {
        public Vector3 WorldPosition;
        public CardinalDirection WorldDirection;
        public PlannedRoom RoomA;   // sala existente (dona do socket aberto)
        public PlannedRoom RoomB;   // sala recém-colocada
    }
}
