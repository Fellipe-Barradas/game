using UnityEngine;
using TMPro;

public class MoedasHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gold;
    [SerializeField] private TextMeshProUGUI silver;
    
    private void OnEnable()
    {
        // se o gerenciador já existe, conecta agora
        if (GerenciadorMoedas.Instancia != null)
            Conectar();
        else
            // senão, tenta de novo no próximo frame
            Invoke(nameof(TentarConectar), 0.1f);
    }
    
    private void OnDisable()
    {
        if (GerenciadorMoedas.Instancia != null)
            GerenciadorMoedas.Instancia.OnMoedasMudaram -= AtualizarTexto;
    }
    
    private void TentarConectar()
    {
        if (GerenciadorMoedas.Instancia != null) Conectar();
    }
    
    private void Conectar()
    {
        var g = GerenciadorMoedas.Instancia;
        g.OnMoedasMudaram += AtualizarTexto;
        AtualizarTexto(g.MoedasDePrata, g.MoedasDeOuro); // valor inicial
    }
    
    private void AtualizarTexto(int prata, int ouro)
    {
        gold.text = $"{ouro}";
        silver.text = $"{prata}";
    }
}