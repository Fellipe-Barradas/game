using UnityEngine;
using UnityEngine.UI;

public class ClassSelectionManager : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private ClassCard[] cards;

    [Header("Botão Confirmar")]
    [SerializeField] private Button confirmButton;

    [Header("Referências")]
    [SerializeField] private MenuController menuController;

    private ClassCard selectedCard;

    void Start()
    {
        foreach (var card in cards)
            card.OnCardClicked += HandleCardClicked;

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

        GameStateManager.Instance.SelectedClass = selectedCard.playerClass;
        GameStateManager.Instance.StartGameplay();
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