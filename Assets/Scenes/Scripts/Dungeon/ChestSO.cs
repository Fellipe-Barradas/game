using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CoinDrop
{
    [Range(0, 100)] public float chance;   // chance de dropar
    public int amount;                      // quantidade se dropar
}

[System.Serializable]
public struct ItemDrop
{
    public ItemSO item;
    [Range(0, 100)] public float chance;
    public int amount;                      // quantidade (>= 1)
}

[CreateAssetMenu(menuName = "Dungeon/Chest", fileName = "Chest")]
public class ChestSO : ScriptableObject
{
    public string displayName;
    public GameObject prefab;               // modelo do baú (visual + Bau + Animator)
    [Tooltip("Peso relativo no sorteio de qual baú aparece.")]
    public float weight = 1f;
    [Tooltip("Profundidade mínima da sala para esse baú aparecer.")]
    public int minDepth = 0;

    [Header("Loot — moedas")]
    public CoinDrop prata;
    public CoinDrop ouro;
    public CoinDrop fragmentos;

    [Header("Loot — itens")]
    public List<ItemDrop> itens = new List<ItemDrop>();
}
