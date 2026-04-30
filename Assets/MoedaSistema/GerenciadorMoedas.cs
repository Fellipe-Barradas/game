using UnityEngine;
using TMPro;

public class GerenciadorMoedas : MonoBehaviour
{
    public static GerenciadorMoedas Instancia;
    
    public int moedasDePrata = 0;
    public int moedasDeOuro = 0;
    
    // Arraste o seu "TextoMoedas" para cá no Inspector
    public TextMeshProUGUI textoInterface;

    void Awake() { Instancia = this; }

    void Start()
    {
        moedasDeOuro = PlayerPrefs.GetInt("MoedasDeOuro", 0);
        AtualizarUI();
    }

    public void AdicionarMoedas(int prata, int ouro)
    {
        moedasDePrata += prata;
        moedasDeOuro += ouro;
        
        // Salva o ouro permanentemente
        PlayerPrefs.SetInt("MoedasDeOuro", moedasDeOuro);
        PlayerPrefs.Save();
        
        AtualizarUI();
    }

    void AtualizarUI()
    {
        if (textoInterface != null)
        {
            textoInterface.text = $"Prata: {moedasDePrata} | Ouro: {moedasDeOuro}";
        }
    }
}