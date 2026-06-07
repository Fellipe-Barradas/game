using System.Collections.Generic;

namespace Game.Dungeon
{
    public class DungeonLayout
    {
        public bool Success;
        public int Seed;
        public List<PlannedRoom> Rooms = new List<PlannedRoom>();
        public List<PlannedSocket> OpenSockets = new List<PlannedSocket>(); // a selar com parede
    }
}
