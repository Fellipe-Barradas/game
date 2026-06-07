using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>Um vão de porta no espaço LOCAL da sala.</summary>
    public struct RoomSocketData
    {
        public Vector3 LocalPosition;
        public CardinalDirection Direction;

        public RoomSocketData(Vector3 localPosition, CardinalDirection direction)
        {
            LocalPosition = localPosition;
            Direction = direction;
        }
    }
}
