using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Planeja o layout (C# puro). Estratégia: colocação gulosa por fronteira de sockets,
    /// selando sockets que não encaixam, com regeneração por seed quando as cotas não fecham
    /// (forma pragmática de backtracking, limitada por maxRegenerations).
    /// </summary>
    public static class DungeonPlanner
    {
        private class FrontierEntry
        {
            public PlannedRoom Owner;
            public PlannedSocket Socket;
        }

        public static DungeonLayout Plan(
            IReadOnlyList<RoomDefinition> catalog,
            DungeonSettings settings,
            int seed,
            int maxRegenerations = 25)
        {
            for (int attempt = 0; attempt < maxRegenerations; attempt++)
            {
                DungeonLayout layout = TryPlan(catalog, settings, seed + attempt);
                if (layout.Success)
                {
                    layout.Seed = seed + attempt;
                    return layout;
                }
            }
            return new DungeonLayout { Success = false, Seed = seed };
        }

        private static DungeonLayout TryPlan(
            IReadOnlyList<RoomDefinition> catalog, DungeonSettings settings, int seed)
        {
            var rng = new System.Random(seed);
            var layout = new DungeonLayout();
            var counts = new Dictionary<RoomType, int>();
            foreach (RoomType t in Enum.GetValues(typeof(RoomType))) counts[t] = 0;

            // 1. Sala de início na origem.
            RoomDefinition startDef = PickByType(catalog, RoomType.Inicio, rng);
            if (startDef == null) return layout; // sucesso=false
            PlannedRoom start = MakePlacedRoom(startDef, 0, Vector3.zero, 0);
            layout.Rooms.Add(start);
            counts[RoomType.Inicio]++;

            var frontier = new List<FrontierEntry>();
            AddOpenSockets(frontier, start);

            int target = rng.Next(settings.MinRooms, settings.MaxRooms + 1);

            // 2. Expansão gulosa.
            while (layout.Rooms.Count < target && frontier.Count > 0)
            {
                int fi = rng.Next(frontier.Count);
                FrontierEntry fe = frontier[fi];
                frontier.RemoveAt(fi);
                if (fe.Socket.Used) continue;

                PlannedRoom placed = TryPlaceAtSocket(catalog, settings, rng, layout, counts, fe);
                if (placed != null)
                {
                    counts[placed.Definition.Type]++;
                    AddOpenSockets(frontier, placed);
                }
                else
                {
                    layout.OpenSockets.Add(fe.Socket); // vira parede
                }
            }

            // 3. Boss na ponta mais profunda.
            if (!TryPlaceBoss(catalog, settings, rng, layout, counts, frontier))
                return layout; // sucesso=false

            // 4. Sela o que sobrou aberto.
            foreach (FrontierEntry fe in frontier)
                if (!fe.Socket.Used) layout.OpenSockets.Add(fe.Socket);

            // 5. Valida cotas mínimas.
            if (!QuotasMet(counts, settings)) return layout; // sucesso=false

            layout.Success = true;
            return layout;
        }

        private static PlannedRoom TryPlaceAtSocket(
            IReadOnlyList<RoomDefinition> catalog, DungeonSettings settings, System.Random rng,
            DungeonLayout layout, Dictionary<RoomType, int> counts, FrontierEntry fe)
        {
            // Tipos elegíveis (exclui Inicio/Boss; respeita MaxQuota), priorizando cotas não cumpridas.
            var types = new List<RoomType> { RoomType.Combate, RoomType.Bau, RoomType.Armadilha, RoomType.Parkour };
            types = types.Where(t => UnderMax(counts, settings, t)).OrderBy(_ => rng.Next()).ToList();
            types = types.OrderByDescending(t => NeedsMore(counts, settings, t)).ToList();

            foreach (RoomType type in types)
            {
                var candidates = catalog.Where(d => d.Type == type).OrderBy(_ => rng.Next()).ToList();
                foreach (RoomDefinition def in candidates)
                {
                    PlannedRoom placed = TryFit(settings, layout, def, fe);
                    if (placed != null)
                        return placed;
                }
            }
            return null;
        }

        private static PlannedRoom TryFit(
            DungeonSettings settings, DungeonLayout layout, RoomDefinition def, FrontierEntry fe)
        {
            // Testa cada socket da candidata como entrada.
            for (int i = 0; i < def.Sockets.Count; i++)
            {
                RoomSocketData entry = def.Sockets[i];
                PlacementMath.PlaceRoom(
                    fe.Socket.WorldPosition, fe.Socket.WorldDirection, entry,
                    out int yaw, out Vector3 t);

                Bounds worldBounds = PlacementMath.RotateBoundsY(def.LocalBounds, yaw);
                worldBounds.center += t;

                bool collides = layout.Rooms.Any(r =>
                    PlacementMath.Overlaps(worldBounds, r.WorldBounds, settings.OverlapMargin));
                if (collides) continue;

                PlannedRoom placed = MakePlacedRoom(def, yaw, t, fe.Owner.Depth + 1);
                // marca o socket de entrada da nova sala e o socket aberto como usados
                placed.Sockets[i].Used = true;
                fe.Socket.Used = true;
                layout.Rooms.Add(placed);
                return placed;
            }
            return null;
        }

        private static bool TryPlaceBoss(
            IReadOnlyList<RoomDefinition> catalog, DungeonSettings settings, System.Random rng,
            DungeonLayout layout, Dictionary<RoomType, int> counts, List<FrontierEntry> frontier)
        {
            if (counts[RoomType.Boss] > 0) return true;

            // sockets abertos ordenados por profundidade do dono (mais fundo primeiro)
            var open = frontier.Where(f => !f.Socket.Used)
                               .OrderByDescending(f => f.Owner.Depth)
                               .ToList();
            var bossDefs = catalog.Where(d => d.Type == RoomType.Boss).OrderBy(_ => rng.Next()).ToList();

            foreach (FrontierEntry fe in open)
            {
                if (fe.Owner.Depth + 1 < settings.MinBossDepth) continue;
                foreach (RoomDefinition def in bossDefs)
                {
                    PlannedRoom placed = TryFit(settings, layout, def, fe);
                    if (placed != null)
                    {
                        counts[RoomType.Boss]++;
                        return true;
                    }
                }
            }
            return false;
        }

        // ---------- helpers ----------

        private static PlannedRoom MakePlacedRoom(RoomDefinition def, int yaw, Vector3 pos, int depth)
        {
            Bounds wb = PlacementMath.RotateBoundsY(def.LocalBounds, yaw);
            wb.center += pos;
            var room = new PlannedRoom
            {
                Definition = def,
                Yaw = yaw,
                Position = pos,
                Depth = depth,
                WorldBounds = wb
            };
            foreach (RoomSocketData s in def.Sockets)
            {
                room.Sockets.Add(new PlannedSocket
                {
                    WorldPosition = PlacementMath.RotateY(s.LocalPosition, yaw) + pos,
                    WorldDirection = CardinalDirections.Rotate(s.Direction, yaw),
                    Used = false
                });
            }
            return room;
        }

        private static void AddOpenSockets(List<FrontierEntry> frontier, PlannedRoom room)
        {
            foreach (PlannedSocket s in room.Sockets)
                if (!s.Used)
                    frontier.Add(new FrontierEntry { Owner = room, Socket = s });
        }

        private static RoomDefinition PickByType(
            IReadOnlyList<RoomDefinition> catalog, RoomType type, System.Random rng)
        {
            var defs = catalog.Where(d => d.Type == type).ToList();
            if (defs.Count == 0) return null;
            float total = defs.Sum(d => Mathf.Max(0.0001f, d.Weight));
            double r = rng.NextDouble() * total;
            foreach (RoomDefinition d in defs)
            {
                r -= Mathf.Max(0.0001f, d.Weight);
                if (r <= 0) return d;
            }
            return defs[defs.Count - 1];
        }

        private static bool UnderMax(Dictionary<RoomType, int> counts, DungeonSettings s, RoomType t)
            => !s.MaxQuota.ContainsKey(t) || counts[t] < s.MaxQuota[t];

        private static bool NeedsMore(Dictionary<RoomType, int> counts, DungeonSettings s, RoomType t)
            => s.MinQuota.ContainsKey(t) && counts[t] < s.MinQuota[t];

        private static bool QuotasMet(Dictionary<RoomType, int> counts, DungeonSettings s)
        {
            if (counts[RoomType.Inicio] != 1) return false;
            if (counts[RoomType.Boss] != 1) return false;
            foreach (var kv in s.MinQuota)
                if (counts[kv.Key] < kv.Value) return false;
            return true;
        }
    }
}
