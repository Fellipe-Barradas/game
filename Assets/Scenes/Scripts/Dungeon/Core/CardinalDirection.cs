using UnityEngine;

namespace Game.Dungeon
{
    public enum CardinalDirection { North = 0, East = 1, South = 2, West = 3 }

    public static class CardinalDirections
    {
        public static CardinalDirection Opposite(CardinalDirection d)
            => (CardinalDirection)(((int)d + 2) % 4);

        /// <summary>Gira a direção por um yaw em graus (múltiplo de 90, sentido horário).</summary>
        public static CardinalDirection Rotate(CardinalDirection d, int yawDegrees)
        {
            int steps = (((yawDegrees / 90) % 4) + 4) % 4;
            return (CardinalDirection)(((int)d + steps) % 4);
        }

        /// <summary>Vetor mundo da direção (North=+Z, East=+X, South=-Z, West=-X).</summary>
        public static Vector3 ToVector(CardinalDirection d)
        {
            switch (d)
            {
                case CardinalDirection.North: return Vector3.forward;
                case CardinalDirection.East: return Vector3.right;
                case CardinalDirection.South: return Vector3.back;
                case CardinalDirection.West: return Vector3.left;
                default: return Vector3.zero;
            }
        }
    }
}
