using UnityEngine;
using TMPro;

public class MoedasHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gold;
    [SerializeField] private TextMeshProUGUI silver;
    [SerializeField] private TextMeshProUGUI fragmentos; // Novo slot para a interface

    private bool conectado = false;

    private void OnEnable()
    {
        TentarConectar();
    }

    private void Update()
    {
        // Cenas separadas: o GerenciadorMoedas (MainScene) pode aparecer depois deste HUD
        // (UIScene). Segue tentando até conseguir, em vez de desistir após um único Invoke.
        if (!conectado)
            TentarConectar();
    }

    private void OnDisable()
    {
        if (conectado && GerenciadorMoedas.Instancia != null)
            GerenciadorMoedas.Instancia.OnRecursosMudaram -= AtualizarTexto;
        conectado = false;
    }

    private void TentarConectar()
    {
        if (conectado || GerenciadorMoedas.Instancia == null) return;
        Conectar();
    }

    private void Conectar()
    {
        var g = GerenciadorMoedas.Instancia;
        g.OnRecursosMudaram += AtualizarTexto; // Nome atualizado
        conectado = true;
        AtualizarTexto(g.MoedasDePrata, g.MoedasDeOuro, g.Fragmentos); // Agora envia os 3
    }
    
    private void AtualizarTexto(int prata, int ouro, int qtdFragmentos)
    {
        // O if (!= null) garante que o jogo não trave se você ainda não tiver criado o UI dos Fragmentos
        if (gold != null) gold.text = $"{ouro}";
        if (silver != null) silver.text = $"{prata}";
        if (fragmentos != null) fragmentos.text = $"{qtdFragmentos}"; 
    }
}