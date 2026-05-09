using UnityEngine;
using System;

public class GerenciadorMoedas : MonoBehaviour
{
    public static GerenciadorMoedas Instancia { get; private set; }
    
    [SerializeField] private int moedasDePrata = 0;
    [SerializeField] private int moedasDeOuro = 0;
    
    public int MoedasDePrata => moedasDePrata;
    public int MoedasDeOuro => moedasDeOuro;
    
    // a UI escuta esse evento
    public event Action<int, int> OnMoedasMudaram;

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
        moedasDeOuro = PlayerPrefs.GetInt("MoedasDeOuro", 0);
        // dispara evento inicial pra UI pegar valor de partida
        OnMoedasMudaram?.Invoke(moedasDePrata, moedasDeOuro);
    }

    public void AdicionarMoedas(int prata, int ouro)
    {
        moedasDePrata += prata;
        moedasDeOuro += ouro;
        OnMoedasMudaram?.Invoke(moedasDePrata, moedasDeOuro);
    }
    
    public bool GastarOuro(int quantidade)
    {
        if (quantidade <= 0 || moedasDeOuro < quantidade) return false;
        moedasDeOuro -= quantidade;
        OnMoedasMudaram?.Invoke(moedasDePrata, moedasDeOuro);
        return true;
    }
    
    public void SalvarProgresso()
    {
        PlayerPrefs.SetInt("MoedasDeOuro", moedasDeOuro);
        PlayerPrefs.Save();
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