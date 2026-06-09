using UnityEngine;

/// <summary>Ponto de spawn de inimigo dentro do prefab da sala (não amarra prefab de inimigo).</summary>
public class EnemyMarker : MonoBehaviour
{
    [Tooltip("Se preenchido, spawna SEMPRE este inimigo aqui (ignora tabela/orçamento). Use para o boss.")]
    public EnemySO enemyOverride;

    private void OnDrawGizmos()
    {
        bool fixo = enemyOverride != null;
        Gizmos.color = fixo ? new Color(1f, 0.3f, 0.85f) : Color.red; // rosa = fixo (boss), vermelho = sorteado
        Gizmos.DrawWireSphere(transform.position, fixo ? 0.7f : 0.4f);
    }
}
