using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Game.Dungeon;

public class DungeonPlannerTests
{
    // Sala quadrada 10x10 com sockets nos 4 lados, no centro de cada parede.
    private static RoomDefinition Square(RoomType type, float weight = 1f)
    {
        var sockets = new List<RoomSocketData>
        {
            new RoomSocketData(new Vector3(0, 0, 5),  CardinalDirection.North),
            new RoomSocketData(new Vector3(5, 0, 0),  CardinalDirection.East),
            new RoomSocketData(new Vector3(0, 0, -5), CardinalDirection.South),
            new RoomSocketData(new Vector3(-5, 0, 0), CardinalDirection.West),
        };
        return new RoomDefinition
        {
            Type = type,
            LocalBounds = new Bounds(Vector3.zero, new Vector3(10, 4, 10)),
            Sockets = sockets,
            Weight = weight,
            PrefabRef = type // marcador qualquer
        };
    }

    private static List<RoomDefinition> Catalog()
    {
        return new List<RoomDefinition>
        {
            Square(RoomType.Inicio),
            Square(RoomType.Combate), Square(RoomType.Combate),
            Square(RoomType.Bau),
            Square(RoomType.Armadilha),
            Square(RoomType.Parkour),
            Square(RoomType.Boss),
        };
    }

    private static DungeonSettings Settings()
    {
        return new DungeonSettings
        {
            MinRooms = 8,
            MaxRooms = 12,
            OverlapMargin = 0.1f,
            MinBossDepth = 2,
            MinQuota = new Dictionary<RoomType, int>
            {
                { RoomType.Combate, 3 }, { RoomType.Bau, 1 },
                { RoomType.Armadilha, 1 }, { RoomType.Parkour, 1 },
            },
            MaxQuota = new Dictionary<RoomType, int>
            {
                { RoomType.Combate, 5 }, { RoomType.Bau, 2 },
                { RoomType.Armadilha, 2 }, { RoomType.Parkour, 1 },
            },
        };
    }

    [Test]
    public void Plan_GeraLayoutComSucesso()
    {
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), Settings(), seed: 1);
        Assert.IsTrue(layout.Success);
        Assert.GreaterOrEqual(layout.Rooms.Count, 2);
    }

    [Test]
    public void Plan_NuncaSobrepoeSalas()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            DungeonLayout layout = DungeonPlanner.Plan(Catalog(), Settings(), seed);
            if (!layout.Success) continue;
            var rooms = layout.Rooms;
            for (int i = 0; i < rooms.Count; i++)
                for (int j = i + 1; j < rooms.Count; j++)
                    Assert.IsFalse(
                        PlacementMath.Overlaps(rooms[i].WorldBounds, rooms[j].WorldBounds, 0.1f),
                        $"Salas {i} e {j} sobrepõem (seed {seed}).");
        }
    }

    [Test]
    public void Plan_TemExatamenteUmInicioEUmBoss()
    {
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), Settings(), seed: 7);
        Assert.IsTrue(layout.Success);
        Assert.AreEqual(1, layout.Rooms.Count(r => r.Definition.Type == RoomType.Inicio));
        Assert.AreEqual(1, layout.Rooms.Count(r => r.Definition.Type == RoomType.Boss));
    }

    [Test]
    public void Plan_BossRespeitaProfundidadeMinima()
    {
        var settings = Settings();
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), settings, seed: 3);
        Assert.IsTrue(layout.Success);
        PlannedRoom boss = layout.Rooms.First(r => r.Definition.Type == RoomType.Boss);
        Assert.GreaterOrEqual(boss.Depth, settings.MinBossDepth);
    }

    [Test]
    public void Plan_RespeitaCotasMinimas()
    {
        var settings = Settings();
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), settings, seed: 11);
        Assert.IsTrue(layout.Success);
        foreach (var kv in settings.MinQuota)
            Assert.GreaterOrEqual(
                layout.Rooms.Count(r => r.Definition.Type == kv.Key), kv.Value);
    }

    [Test]
    public void Plan_RespeitaCotasMaximas()
    {
        var settings = Settings();
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), settings, seed: 11);
        Assert.IsTrue(layout.Success);
        foreach (var kv in settings.MaxQuota)
            Assert.LessOrEqual(
                layout.Rooms.Count(r => r.Definition.Type == kv.Key), kv.Value);
    }

    [Test]
    public void Plan_MesmoSeedGeraLayoutIdentico()
    {
        DungeonLayout a = DungeonPlanner.Plan(Catalog(), Settings(), seed: 42);
        DungeonLayout b = DungeonPlanner.Plan(Catalog(), Settings(), seed: 42);
        Assert.AreEqual(a.Rooms.Count, b.Rooms.Count);
        Assert.AreEqual(a.Seed, b.Seed);
        for (int i = 0; i < a.Rooms.Count; i++)
        {
            Assert.AreEqual(a.Rooms[i].Definition.Type, b.Rooms[i].Definition.Type);
            Assert.AreEqual(a.Rooms[i].Position, b.Rooms[i].Position);
            Assert.AreEqual(a.Rooms[i].Yaw, b.Rooms[i].Yaw);
        }
    }
}
