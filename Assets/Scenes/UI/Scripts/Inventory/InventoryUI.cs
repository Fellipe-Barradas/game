using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventorySlotParent;
    [SerializeField] private GameObject hotbarSlotParent;
    [SerializeField] private Image dragImage;

    private Slot[] slotsUI;
    private Inventory inventory;
    private int draggedSlotIndex = -1;
    private int hotbarStartIndex;
    private int hotbarSize;

    public bool IsDragging => draggedSlotIndex >= 0;
    public bool IsInventoryOpen => gameObject.activeInHierarchy;

    private void Awake()
    {
        var invSlots = inventorySlotParent.GetComponentsInChildren<Slot>(includeInactive: true);
        var hotSlots = hotbarSlotParent != null
            ? hotbarSlotParent.GetComponentsInChildren<Slot>(includeInactive: true)
            : new Slot[0];

        hotbarStartIndex = invSlots.Length;
        hotbarSize = hotSlots.Length;

        slotsUI = new Slot[invSlots.Length + hotSlots.Length];
        invSlots.CopyTo(slotsUI, 0);
        hotSlots.CopyTo(slotsUI, invSlots.Length);

        for (int i = 0; i < slotsUI.Length; i++)
            slotsUI[i].SlotIndex = i;

        TryHookInventory();
    }

    private void OnEnable()
    {
        if (inventory != null)
            Redraw();
    }

    private void OnDisable()
    {
        draggedSlotIndex = -1;
        if (dragImage != null) dragImage.enabled = false;
    }

    private void TryHookInventory()
    {
        if (inventory != null) return;

        inventory = FindFirstObjectByType<Inventory>();
        if (inventory != null)
        {
            inventory.Initialize(slotsUI.Length);
            inventory.SetHotbarInfo(hotbarStartIndex, hotbarSize);
            foreach (var slot in slotsUI)
                slot.SetInventoryUI(this);
            inventory.OnInventoryChanged += Redraw;
            inventory.OnEquippedChanged += UpdateHotbarOpacity;
            UpdateHotbarOpacity();
        }
    }

    // --- Drag ---

    public void UsarItem(int slotIndex) {
        // Procura o inventário no Player
        Inventory inv = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
        if (inv != null) {
            inv.UsarItem(slotIndex);
            // Não precisas de UpdateSlots() aqui, pois o Inventory.cs 
            // já dispara o OnInventoryChanged que a tua UI ouve.
        }
    }

    private Slot GetHoveredSlot()
    {
        foreach (var slot in slotsUI)
            if (slot.hovering) return slot;
        return null;
    }

    public void BeginDrag(int slotIndex, Sprite icon)
    {
        draggedSlotIndex = slotIndex;
        if (dragImage != null)
        {
            dragImage.sprite = icon;
            dragImage.enabled = true;
        }
    }

    public void UpdateDragPosition(Vector2 screenPos)
    {
        if (dragImage != null)
            dragImage.rectTransform.position = screenPos;
    }

    public void EndDrag()
    {
        if (IsDragging)
        {
            Slot target = GetHoveredSlot();
            if (target != null)
                inventory.MoveItem(draggedSlotIndex, target.SlotIndex);
        }

        draggedSlotIndex = -1;
        if (dragImage != null) dragImage.enabled = false;
        UpdateHotbarOpacity();
    }

    // --- Hotbar ---

    private void UpdateHotbarOpacity()
    {
        if (inventory == null || hotbarSize == 0) return;
        for (int i = 0; i < hotbarSize; i++)
        {
            float alpha = i == inventory.EquippedHotbarIndex
                ? inventory.EquipedOpacity
                : inventory.NormalOpacity;
            slotsUI[hotbarStartIndex + i].SetAlpha(alpha);
        }
    }

    // --- Redraw ---

    private void Redraw()
    {
        for (int i = 0; i < slotsUI.Length; i++)
        {
            if (i < inventory.Entries.Count && inventory.Entries[i].item != null)
                slotsUI[i].SetItem(inventory.Entries[i].item, inventory.Entries[i].amount);
            else
                slotsUI[i].ClearSlot();
        }
    }
}
