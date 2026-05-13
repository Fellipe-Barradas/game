using UnityEngine;

public class UpgradeArma : MonoBehaviour
{
    [Header("Configuração de Custo")]
    public int custoOuro = 1;       // Novo custo definido
    public int custoFragmentos = 5; // Novo custo definido
    public int incrementoDano = 80;

    public void TentarUpgrade()
    {
        // Acessa o Singleton de moedas
        var gm = GerenciadorMoedas.Instancia;

        if (gm == null) return;

        // Validação de múltiplos recursos (Ouro E Fragmentos)
        if (gm.MoedasDeOuro >= custoOuro && gm.Fragmentos >= custoFragmentos)
        {
            // Executa a transação atômica
            gm.GastarOuro(custoOuro);
            gm.GastarFragmentos(custoFragmentos);

            // Injeta o bônus no sistema de combate
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent(out CombatScript combat))
            {
                combat.bonusDanoUpgrade += incrementoDano;
                Debug.Log($"[UPGRADE] Sucesso! Novo bônus acumulado: +{combat.bonusDanoUpgrade} de dano.");
            }
        }
        else
        {
            Debug.Log($"Recursos insuficientes! Você precisa de {custoOuro} Ouro e {custoFragmentos} Fragmentos.");
        }
    }
}