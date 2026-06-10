using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

[CreateAssetMenu(menuName = "Dungeon/Dungeon Profile", fileName = "DungeonProfile")]
public class DungeonProfile : ScriptableObject
{
    [Header("Quantidade de salas")]
    public int minRooms = 8;
    public int maxRooms = 14;

    [Header("Geometria")]
    public float overlapMargin = 0.1f;
    public int minBossDepth = 3;

    [System.Serializable]
    public class Quota { public RoomType type; public int min; public int max; }

    public List<Quota> quotas = new List<Quota>
    {
        new Quota { type = RoomType.Combate,   min = 3, max = 5 },
        new Quota { type = RoomType.Bau,       min = 1, max = 2 },
        new Quota { type = RoomType.Armadilha, min = 1, max = 2 },
        new Quota { type = RoomType.Parkour,   min = 1, max = 1 },
    };

    public DungeonSettings ToSettings()
    {
        var s = new DungeonSettings
        {
            MinRooms = minRooms,
            MaxRooms = maxRooms,
            OverlapMargin = overlapMargin,
            MinBossDepth = minBossDepth,
            MinQuota = new Dictionary<RoomType, int>(),
            MaxQuota = new Dictionary<RoomType, int>(),
        };
        foreach (Quota q in quotas)
        {
            s.MinQuota[q.type] = q.min;
            s.MaxQuota[q.type] = q.max;
        }
        return s;
    }
}
