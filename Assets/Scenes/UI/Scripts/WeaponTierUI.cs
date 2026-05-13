using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponTierUI : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;

    [Header("Textos Configuráveis")]
    [SerializeField] private string tierPrefix = "Arma Tier ";
    [SerializeField] private string costPrefix = "Custo: ";
    [SerializeField] private string costSuffix = " Shards";
    [SerializeField] private string maxLabel = "MAX";

    private WeaponTierManager tierManager;

    private void Start()
    {
        tierManager = WeaponTierManager.Instance;
        if (tierManager == null)
        {
            Debug.LogError("[WeaponTierUI] WeaponTierManager.Instance não encontrado. Adicione WeaponTierManager ao Player.");
            return;
        }

        tierManager.OnTierChanged += Refresh;
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        else
            Debug.LogError("[WeaponTierUI] upgradeButton não assignado no Inspector.", this);
        Refresh(tierManager.CurrentTier);
    }

    private void OnDestroy()
    {
        if (tierManager != null)
            tierManager.OnTierChanged -= Refresh;
    }

    private void OnUpgradeClicked() => tierManager.TryUpgrade();

    private void Refresh(int tier)
    {
        tierText.text = tierPrefix + tier;

        if (tierManager.IsMaxTier)
        {
            costText.text = maxLabel;
            upgradeButton.interactable = false;
        }
        else
        {
            costText.text = costPrefix + tierManager.UpgradeCost + costSuffix;
            upgradeButton.interactable = true;
        }
    }
}
