using UnityEngine;
using System;

public class GerenciadorMoedas : MonoBehaviour
{
    public static GerenciadorMoedas Instancia { get; private set; }
    
    [Header("Sessão Atual (Reseta ao ir pro Menu)")]
    [SerializeField] private int moedasDePrata = 0;

    [Header("Persistente (Salvo)")]
    [SerializeField] private int moedasDeOuro = 0;      // Global, vale para o jogo todo
    [SerializeField] private int fragmentos = 0;        // Por classe selecionada

    private const string CHAVE_OURO = "MoedasDeOuro";
    // Fragmentos são salvos por classe (espelha o padrão do WeaponTierManager)
    private string ChaveFragmentos =>
        "Fragmentos_" + (GameStateManager.Instance != null
            ? GameStateManager.Instance.SelectedClass.ToString()
            : PlayerClass.Espadachim.ToString());

    // Acesso ao ouro salvo sem precisar de instância (usado no menu de seleção de classe).
    public static int OuroSalvo => PlayerPrefs.GetInt(CHAVE_OURO, 0);

    public static bool GastarOuroSalvo(int quantidade)
    {
        int atual = PlayerPrefs.GetInt(CHAVE_OURO, 0);
        if (quantidade <= 0 || atual < quantidade) return false;
        PlayerPrefs.SetInt(CHAVE_OURO, atual - quantidade);
        PlayerPrefs.Save();
        return true;
    }

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
        // Ouro (global) e Fragmentos (por classe) são carregados do disco
        moedasDeOuro = PlayerPrefs.GetInt(CHAVE_OURO, 0);
        fragmentos = PlayerPrefs.GetInt(ChaveFragmentos, 0);

        // Prata começa zerada toda vez que a cena carrega
        moedasDePrata = 0;

        NotificarUI();
    }

    // Método único para receber os drops dos Inimigos e Baús
    public void AdicionarDrops(int prata, int ouro, int qtdFragmentos)
    {
        moedasDePrata += prata;
        moedasDeOuro += ouro;
        fragmentos += qtdFragmentos;

        SalvarProgresso();
        NotificarUI();
    }

    public bool GastarOuro(int quantidade)
    {
        if (quantidade <= 0 || moedasDeOuro < quantidade) return false;
        moedasDeOuro -= quantidade;
        SalvarProgresso();
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
        SalvarProgresso();
        NotificarUI();
        return true;
    }

    public void SalvarProgresso()
    {
        // Ouro é global; Fragmentos são por classe
        PlayerPrefs.SetInt(CHAVE_OURO, moedasDeOuro);
        PlayerPrefs.SetInt(ChaveFragmentos, fragmentos);
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