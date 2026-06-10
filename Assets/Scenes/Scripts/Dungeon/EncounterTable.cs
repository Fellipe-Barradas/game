using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Encounter Table", fileName = "EncounterTable")]
public class EncounterTable : ScriptableObject
{
    public List<EnemySO> enemies = new List<EnemySO>();

    [Header("Orçamento por sala")]
    public int baseBudget = 2;
    [Tooltip("Orçamento adicional por unidade de profundidade.")]
    public int budgetPerDepth = 1;

    public int BudgetForDepth(int depth) => baseBudget + budgetPerDepth * depth;

    /// <summary>Sorteia um inimigo elegível (depth/budget) via rng; null se nenhum couber.</summary>
    public EnemySO Pick(int depth, int remainingBudget, System.Random rng)
    {
        var eligible = new List<EnemySO>();
        float total = 0f;
        foreach (EnemySO e in enemies)
        {
            if (e == null || e.prefab == null) continue;
            if (e.minDepth > depth) continue;
            if (e.budgetCost > remainingBudget) continue;
            eligible.Add(e);
            total += Mathf.Max(0.0001f, e.weight);
        }
        if (eligible.Count == 0) return null;

        double r = rng.NextDouble() * total;
        foreach (EnemySO e in eligible)
        {
            r -= Mathf.Max(0.0001f, e.weight);
            if (r <= 0) return e;
        }
        return eligible[eligible.Count - 1];
    }
}
