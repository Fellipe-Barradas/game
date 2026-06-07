using System.Collections.Generic;

namespace Game.Dungeon
{
    /// <summary>Parâmetros puros (sem Unity SO) para o planner ser testável.</summary>
    public class DungeonSettings
    {
        public int MinRooms = 8;
        public int MaxRooms = 14;
        public float OverlapMargin = 0.1f;
        public int MinBossDepth = 3;

        /// <summary>Mínimo exigido por tipo (validação ao final). Inicio/Boss tratados à parte.</summary>
        public Dictionary<RoomType, int> MinQuota = new Dictionary<RoomType, int>
        {
            { RoomType.Combate, 3 },
            { RoomType.Bau, 1 },
            { RoomType.Armadilha, 1 },
            { RoomType.Parkour, 1 },
        };

        /// <summary>Máximo por tipo (corte durante a colocação).</summary>
        public Dictionary<RoomType, int> MaxQuota = new Dictionary<RoomType, int>
        {
            { RoomType.Combate, 5 },
            { RoomType.Bau, 2 },
            { RoomType.Armadilha, 2 },
            { RoomType.Parkour, 1 },
        };
    }
}
