using UnityEngine;

[CreateAssetMenu(fileName = "WeaponTierConfig", menuName = "Game/Weapon Tier Config")]
public class WeaponTierConfig : ScriptableObject
{
    [System.Serializable]
    public struct TierData
    {
        [Min(1)] public int tier;
        [Min(0f)] public float damageMultiplier;
        [Min(0)] public int shardCost;
    }

    [SerializeField] private TierData[] tiers = new TierData[]
    {
        new TierData { tier = 1, damageMultiplier = 1.0f, shardCost = 0 },
        new TierData { tier = 2, damageMultiplier = 1.5f, shardCost = 1 },
        new TierData { tier = 3, damageMultiplier = 2.0f, shardCost = 2 },
        new TierData { tier = 4, damageMultiplier = 3.0f, shardCost = 4 },
        new TierData { tier = 5, damageMultiplier = 5.0f, shardCost = 8 },
    };

    public int TierCount => tiers.Length;

    public TierData GetTierData(int tier)
    {
        int index = Mathf.Clamp(tier - 1, 0, tiers.Length - 1);
        return tiers[index];
    }

    public float GetMultiplier(int tier) => GetTierData(tier).damageMultiplier;

    // Retorna o custo para subir DO tier atual para o próximo
    public int GetUpgradeCost(int currentTier)
    {
        if (currentTier >= TierCount) return 0;
        return GetTierData(currentTier + 1).shardCost;
    }
}
