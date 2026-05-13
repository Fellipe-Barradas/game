using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public class Slot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerClickHandler // <- 1. ADICIONADO AQUI PARA DETECTAR CLIQUES
{
    public bool hovering;
    public int SlotIndex { get; set; }

    private ItemSO heltItem;
    private int itemAmount;
    private Image iconImage;
    private TextMeshProUGUI amountTxt;
    private CanvasGroup canvasGroup;
    private InventoryUI inventoryUI;

    private void Awake()
    {
        iconImage = transform.GetChild(0).GetComponent<Image>();
        amountTxt = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetInventoryUI(InventoryUI ui) => inventoryUI = ui;
    public void SetAlpha(float alpha) => canvasGroup.alpha = alpha;

    public ItemSO GetItem() => heltItem;
    public int GetAmount() => itemAmount;
    public bool HasItem() => heltItem != null;

    public void SetItem(ItemSO item, int amount = 1)
    {
        heltItem = item;
        itemAmount = amount;
        UpdateSlot();
    }

    // 2. SUBSTITUÍDO: Lógica nativa da Unity para cliques na interface
    public void OnPointerClick(PointerEventData eventData)
    {
        // Verifica se clicou com o botão esquerdo e se o slot não está vazio
        if (eventData.button == PointerEventData.InputButton.Left && HasItem())
        {
            // Busca o Inventário no Player e manda usar o item deste slot específico
            Inventory inv = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
            if (inv != null)
            {
                inv.UsarItem(SlotIndex);
            }
        }
    }
    public void UpdateSlot()
    {
        if (heltItem != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = heltItem.icon;
            amountTxt.text = itemAmount.ToString();
        }
        else
        {
            iconImage.enabled = false;
            amountTxt.text = "";
        }
    }

    public void ClearSlot()
    {
        heltItem = null;
        itemAmount = 0;
        UpdateSlot();
    }

    public void OnPointerEnter(PointerEventData eventData) => hovering = true;
    public void OnPointerExit(PointerEventData eventData) => hovering = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem() || inventoryUI == null || !inventoryUI.IsInventoryOpen) { eventData.pointerDrag = null; return; }
        inventoryUI.BeginDrag(SlotIndex, heltItem.icon);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.4f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventoryUI.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        inventoryUI.EndDrag();
    }
}