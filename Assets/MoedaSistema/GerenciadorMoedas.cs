using UnityEngine;
using System;

public class GerenciadorMoedas : MonoBehaviour
{
    public static GerenciadorMoedas Instancia { get; private set; }
    
    [Header("Sessão Atual (Reseta ao ir pro Menu)")]
    [SerializeField] private int moedasDePrata = 0;
    [SerializeField] private int fragmentos = 0;

    [Header("Persistente (Salvo)")]
    [SerializeField] private int moedasDeOuro = 0;
    
    public int MoedasDePrata => moedasDePrata;
    public int MoedasDeOuro => moedasDeOuro;
    public int Fragmentos => fragmentos;
    
    // Atualizado para enviar os 3 valores para o HUD: Prata, Ouro, Fragmentos
    public event Action<int, int, int> OnRecursosMudaram;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    private void Start()
    {
        // Ouro é carregado do disco (PlayerPrefs)
        moedasDeOuro = PlayerPrefs.GetInt("MoedasDeOuro", 0);
        
        // Prata e Fragmentos começam zerados toda vez que a cena carrega
        moedasDePrata = 0; 
        fragmentos = 0;
        
        NotificarUI();
    }

    // Método único para receber os drops dos Inimigos e Baús
    public void AdicionarDrops(int prata, int ouro, int qtdFragmentos)
    {
        moedasDePrata += prata;
        moedasDeOuro += ouro;
        fragmentos += qtdFragmentos;
        
        NotificarUI();
    }
    
    public bool GastarOuro(int quantidade)
    {
        if (quantidade <= 0 || moedasDeOuro < quantidade) return false;
        moedasDeOuro -= quantidade;
        NotificarUI();
        return true;
    }

    public bool GastarPrata(int quantidade)
    {
        if (quantidade <= 0 || moedasDePrata < quantidade) return false;
        moedasDePrata -= quantidade;
        NotificarUI();
        return true;
    }

    public bool GastarFragmentos(int quantidade)
    {
        if (quantidade <= 0 || fragmentos < quantidade) return false;
        fragmentos -= quantidade;
        NotificarUI();
        return true;
    }
    
    public void SalvarProgresso()
    {
        // Apenas o Ouro é gravado no disco
        PlayerPrefs.SetInt("MoedasDeOuro", moedasDeOuro);
        PlayerPrefs.Save();
    }
    
    private void NotificarUI()
    {
        OnRecursosMudaram?.Invoke(moedasDePrata, moedasDeOuro, fragmentos);
    }

    private void OnApplicationPause(bool pausou)
    {
        if (pausou) SalvarProgresso();
    }
    
    private void OnApplicationQuit()
    {
        SalvarProgresso();
    }
}