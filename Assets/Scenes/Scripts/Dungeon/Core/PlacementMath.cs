using UnityEngine;

namespace Game.Dungeon
{
    public static class PlacementMath
    {
        /// <summary>Rotaciona um ponto em torno de Y por um yaw múltiplo de 90° (horário, igual ao Unity).</summary>
        public static Vector3 RotateY(Vector3 p, int yawDegrees)
        {
            int steps = (((yawDegrees / 90) % 4) + 4) % 4;
            switch (steps)
            {
                case 1: return new Vector3(p.z, p.y, -p.x);
                case 2: return new Vector3(-p.x, p.y, -p.z);
                case 3: return new Vector3(-p.z, p.y, p.x);
                default: return p;
            }
        }

        /// <summary>Rotaciona bounds AABB em torno de Y (90/270 troca extents X/Z).</summary>
        public static Bounds RotateBoundsY(Bounds b, int yawDegrees)
        {
            int steps = (((yawDegrees / 90) % 4) + 4) % 4;
            Vector3 center = RotateY(b.center, yawDegrees);
            Vector3 size = (steps % 2 == 0)
                ? b.size
                : new Vector3(b.size.z, b.size.y, b.size.x);
            return new Bounds(center, size);
        }

        /// <summary>Menor yaw (0/90/180/270) tal que entryDir girado vire targetDir.</summary>
        public static int SolveYaw(CardinalDirection entryDir, CardinalDirection targetDir)
        {
            for (int steps = 0; steps < 4; steps++)
                if (CardinalDirections.Rotate(entryDir, steps * 90) == targetDir)
                    return steps * 90;
            return 0;
        }

        /// <summary>
        /// Dado um socket aberto (posição+direção em mundo) e o socket de entrada de uma sala candidata,
        /// retorna o yaw e a translação que encostam os dois sockets (direções opostas).
        /// </summary>
        public static void PlaceRoom(
            Vector3 openWorldPos, CardinalDirection openWorldDir,
            RoomSocketData entrySocket,
            out int yaw, out Vector3 translation)
        {
            CardinalDirection target = CardinalDirections.Opposite(openWorldDir);
            yaw = SolveYaw(entrySocket.Direction, target);
            Vector3 rotatedEntry = RotateY(entrySocket.LocalPosition, yaw);
            translation = openWorldPos - rotatedEntry;
        }

        /// <summary>Overlap AABB com margem (encolhe XZ para permitir parede compartilhada). Y mantido.</summary>
        public static bool Overlaps(Bounds a, Bounds b, float margin)
        {
            Vector3 shrink = new Vector3(margin * 2f, 0f, margin * 2f);
            Bounds aa = new Bounds(a.center, a.size - shrink);
            Bounds bb = new Bounds(b.center, b.size - shrink);
            return aa.Intersects(bb);
        }
    }
}
