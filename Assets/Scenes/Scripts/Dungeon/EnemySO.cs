using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Enemy", fileName = "Enemy")]
public class EnemySO : ScriptableObject
{
    public string displayName;
    public GameObject prefab;
    [Tooltip("Peso relativo no sorteio.")]
    public float weight = 1f;
    [Tooltip("Custo no orçamento da sala (inimigos mais fortes custam mais).")]
    public int budgetCost = 1;
    [Tooltip("Profundidade mínima da sala para esse inimigo aparecer.")]
    public int minDepth = 0;
}
