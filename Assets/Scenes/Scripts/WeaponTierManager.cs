using System;
using UnityEngine;

public class WeaponTierManager : MonoBehaviour
{
    public static WeaponTierManager Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private WeaponTierConfig config;

    [Header("Feedback (opcional)")]
    [SerializeField] private GameObject feedbackInsuficiente;

    private string SaveKey => "WeaponTier_" + GameStateManager.Instance.SelectedClass;

    public int CurrentTier { get; private set; } = 1;
    public int MaxTier => config.TierCount;
    public bool IsMaxTier => CurrentTier >= MaxTier;
    public int UpgradeCost => config.GetUpgradeCost(CurrentTier);
    public float CurrentMultiplier => config.GetMultiplier(CurrentTier);

    // Disparado sempre que o tier muda (UI e outros sistemas escutam)
    public event Action<int> OnTierChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        if (config == null) { Debug.LogError("[WeaponTierManager] config não assignado no Inspector.", this); return; }
        Instance = this;
    }

    private void Start()
    {
        CurrentTier = PlayerPrefs.GetInt(SaveKey, 1);
        ApplyTierToCombat();
        OnTierChanged?.Invoke(CurrentTier);
    }

    public bool TryUpgrade()
    {
        if (IsMaxTier) return false;

        var gm = GerenciadorMoedas.Instancia;
        if (gm == null) return false;

        int cost = UpgradeCost;
        if (!gm.GastarFragmentos(cost))
        {
            Debug.Log($"[WeaponTier] Fragmentos insuficientes. Necessário: {cost}, disponível: {gm.Fragmentos}");
            if (feedbackInsuficiente != null)
            {
                feedbackInsuficiente.SetActive(true);
                Invoke(nameof(HideFeedback), 2f);
            }
            return false;
        }

        CurrentTier++;
        PlayerPrefs.SetInt(SaveKey, CurrentTier);
        PlayerPrefs.Save();
        ApplyTierToCombat();
        OnTierChanged?.Invoke(CurrentTier);
        Debug.Log($"[WeaponTier] Upgrade! Tier {CurrentTier}, multiplicador: {CurrentMultiplier}x");
        return true;
    }

    private void ApplyTierToCombat()
    {
        CombatScript combat = GetComponentInChildren<CombatScript>();
        if (combat != null)
            combat.tierDamageMultiplier = CurrentMultiplier;
        else
            Debug.LogWarning("[WeaponTierManager] CombatScript não encontrado no Player ou seus filhos.", this);
    }

    private void HideFeedback()
    {
        if (feedbackInsuficiente != null) feedbackInsuficiente.SetActive(false);
    }
}
