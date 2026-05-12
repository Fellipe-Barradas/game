using UnityEngine;
using TMPro;

public class MoedasHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gold;
    [SerializeField] private TextMeshProUGUI silver;
    [SerializeField] private TextMeshProUGUI fragmentos; // Novo slot para a interface
    
    private void OnEnable()
    {
        if (GerenciadorMoedas.Instancia != null)
            Conectar();
        else
            Invoke(nameof(TentarConectar), 0.1f);
    }
    
    private void OnDisable()
    {
        if (GerenciadorMoedas.Instancia != null)
            GerenciadorMoedas.Instancia.OnRecursosMudaram -= AtualizarTexto; // Nome atualizado
    }
    
    private void TentarConectar()
    {
        if (GerenciadorMoedas.Instancia != null) Conectar();
    }
    
    private void Conectar()
    {
        var g = GerenciadorMoedas.Instancia;
        g.OnRecursosMudaram += AtualizarTexto; // Nome atualizado
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