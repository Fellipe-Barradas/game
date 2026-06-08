using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

[CreateAssetMenu(menuName = "Dungeon/Chest Table", fileName = "ChestTable")]
public class ChestTable : ScriptableObject
{
    public List<ChestSO> chests = new List<ChestSO>();

    /// <summary>Sorteia um ChestSO elegível (minDepth &lt;= depth) ponderado por weight. null se nenhum.</summary>
    public ChestSO Pick(int depth, System.Random rng)
    {
        var eligible = new List<ChestSO>();
        var weights = new List<float>();
        foreach (ChestSO c in chests)
        {
            if (c == null || c.prefab == null) continue;
            if (c.minDepth > depth) continue;
            eligible.Add(c);
            weights.Add(c.weight);
        }
        if (eligible.Count == 0) return null;

        int idx = WeightedPicker.PickIndex(weights, rng.NextDouble());
        return idx >= 0 ? eligible[idx] : null;
    }
}
