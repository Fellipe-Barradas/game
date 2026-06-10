using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>Popula uma sala instanciada a partir dos markers, usando a EncounterTable.</summary>
public class RoomPopulator : MonoBehaviour
{
    public EncounterTable encounterTable;
    public ChestTable chestTable;

    /// <summary>
    /// Popula a sala. roomInstance = GameObject instanciado; type/depth vêm do PlannedRoom;
    /// rng compartilhado garante determinismo por seed.
    /// </summary>
    public void Populate(GameObject roomInstance, RoomType type, int depth, System.Random rng)
    {
        var spawnedEnemies = new List<GameObject>();

        var enemyMarkers = roomInstance.GetComponentsInChildren<EnemyMarker>(true);

        // 1. Markers com override: spawna SEMPRE o inimigo escolhido (ex.: boss), fora do orçamento.
        foreach (EnemyMarker m in enemyMarkers)
        {
            if (m.enemyOverride == null || m.enemyOverride.prefab == null) continue;
            GameObject e = Instantiate(m.enemyOverride.prefab, m.transform.position, m.transform.rotation, roomInstance.transform);
            spawnedEnemies.Add(e);
        }

        // 2. Markers normais: sorteio por orçamento/profundidade na EncounterTable.
        if (encounterTable != null)
        {
            int budget = encounterTable.BudgetForDepth(depth);
            foreach (EnemyMarker m in enemyMarkers)
            {
                if (m.enemyOverride != null) continue; // já tratado acima
                if (budget <= 0) break;
                EnemySO pick = encounterTable.Pick(depth, budget, rng);
                if (pick == null) break;
                budget -= pick.budgetCost;
                GameObject e = Instantiate(pick.prefab, m.transform.position, m.transform.rotation, roomInstance.transform);
                spawnedEnemies.Add(e);
            }
        }

        // Baús.
        foreach (ChestMarker c in roomInstance.GetComponentsInChildren<ChestMarker>(true))
        {
            // PROB #1 — esta posição vira baú?
            if (rng.NextDouble() * 100.0 >= c.spawnChance) continue;

            // PROB #2 — qual baú? (override do marker tem prioridade sobre a tabela)
            ChestSO pick = c.chestOverride != null
                ? c.chestOverride
                : (chestTable != null ? chestTable.Pick(depth, rng) : null);
            if (pick == null || pick.prefab == null) continue;

            GameObject go = Instantiate(pick.prefab, c.transform.position, c.transform.rotation, roomInstance.transform);
            if (go.TryGetComponent<Bau>(out var bau)) bau.data = pick;
        }

        // Armadilhas com override (as fixas já vêm no prefab).
        foreach (TrapMarker t in roomInstance.GetComponentsInChildren<TrapMarker>(true))
        {
            if (t.trapPrefabOverride != null)
                Instantiate(t.trapPrefabOverride, t.transform.position, t.transform.rotation, roomInstance.transform);
        }

        // Liga o controlador de sala.
        RoomController rc = roomInstance.GetComponent<RoomController>();
        if (rc == null) rc = roomInstance.AddComponent<RoomController>();
        rc.Configure(spawnedEnemies, lockDoors: type == RoomType.Combate || type == RoomType.Boss);
    }
}
