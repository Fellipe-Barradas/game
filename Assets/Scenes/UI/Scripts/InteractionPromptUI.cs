using UnityEngine;
using TMPro;

// Singleton: existe um único prompt na HUD, qualquer objeto pode acioná-lo
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private GameObject promptRoot;   // o painel todo (KeyBackground + texto)
    [SerializeField] private TextMeshProUGUI actionText; // o texto "to open", "to pick up", etc.

    [Header("Prefixo do texto")]
    [SerializeField] private string prefix = "TO";    // resulta em "TO OPEN", "TO PICK UP"

    void Awake()
    {
        // Garante apenas uma instância
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Começa escondido
        if (promptRoot != null) promptRoot.SetActive(false);
    }

    public void Show(string actionLabel)
    {
        if (promptRoot == null) return;

        promptRoot.SetActive(true);
        if (actionText != null)
            actionText.text = $"{prefix} {actionLabel.ToUpper()}";
    }

    public void Hide()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
    }
}