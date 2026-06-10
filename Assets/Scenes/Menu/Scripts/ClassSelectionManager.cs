using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClassSelectionManager : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private ClassCard[] cards;

    [Header("Botão Confirmar")]
    [SerializeField] private Button confirmButton;

    [Header("Referências")]
    [SerializeField] private MenuController menuController;

    [Header("Compra de Classes")]
    [SerializeField] private PopupConfirmacao popupConfirmacao;
    [SerializeField] private TMP_Text ouroLabel;

    private ClassCard selectedCard;

    void Start()
    {
        foreach (var card in cards)
        {
            card.OnCardClicked += HandleCardClicked;
            card.OnComprarClicked += HandleComprarClicked;
        }

        AtualizarOuroLabel();

        confirmButton.interactable = false;
        confirmButton.onClick.AddListener(ConfirmSelection);
    }

    private void HandleCardClicked(ClassCard clicked)
    {
        foreach (var card in cards)
            card.SetSelected(card == clicked);

        selectedCard = clicked;
        confirmButton.interactable = true;
    }

    private void ConfirmSelection()
    {
        if (selectedCard == null) return;

        // Salva a classe E a arma no StateManager!
        GameStateManager.Instance.SelectedClass = selectedCard.playerClass;
        GameStateManager.Instance.SelectedWeapon = selectedCard.classWeapon; // Adicione isso no seu GameStateManager

        GameStateManager.Instance.StartGameplay();
    }

    private void HandleComprarClicked(ClassCard card)
    {
        bool podeComprar = GerenciadorMoedas.OuroSalvo >= card.preco;
        string msg = podeComprar
            ? $"Comprar {card.playerClass} por {card.preco} ouro?"
            : $"Ouro insuficiente ({GerenciadorMoedas.OuroSalvo}/{card.preco})";

        popupConfirmacao.Mostrar(msg, podeComprar, () => ComprarClasse(card));
    }

    private void ComprarClasse(ClassCard card)
    {
        if (!GerenciadorMoedas.GastarOuroSalvo(card.preco)) return;

        ProgressaoClasses.Desbloquear(card.playerClass);
        card.MarcarComoDesbloqueada();
        AtualizarOuroLabel();
    }

    private void AtualizarOuroLabel()
    {
        if (ouroLabel != null)
            ouroLabel.text = GerenciadorMoedas.OuroSalvo + " ouro";
    }

    public void OnReturnClicked()
    {
        selectedCard = null;
        confirmButton.interactable = false;
        foreach (var card in cards)
            card.SetSelected(false);

        menuController.IrParaMenuInicial();
    }
}   