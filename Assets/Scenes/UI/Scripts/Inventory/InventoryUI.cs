using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventorySlotParent;
    [SerializeField] private GameObject hotbarSlotParent;
    [SerializeField] private Image dragImage;

    private Slot[] slotsUI;
    private Inventory inventory;
    private int draggedSlotIndex = -1;

    public bool IsDragging => draggedSlotIndex >= 0;

    private void Awake()
    {
        var invSlots = inventorySlotParent.GetComponentsInChildren<Slot>(includeInactive: true);
        var hotSlots = hotbarSlotParent != null
            ? hotbarSlotParent.GetComponentsInChildren<Slot>(includeInactive: true)
            : new Slot[0];

        slotsUI = new Slot[invSlots.Length + hotSlots.Length];
        invSlots.CopyTo(slotsUI, 0);
        hotSlots.CopyTo(slotsUI, invSlots.Length);

        for (int i = 0; i < slotsUI.Length; i++)
            slotsUI[i].SlotIndex = i;
    }

    private void Start()
    {
        TryHookInventory();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged += Redraw;
            Redraw();
        }
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Redraw;
    }

    private void TryHookInventory()
    {
        if (inventory != null) return;

        inventory = FindObjectOfType<Inventory>();
        if (inventory != null)
        {
            inventory.Initialize(slotsUI.Length);
            foreach (var slot in slotsUI)
                slot.SetInventoryUI(this);
            inventory.OnInventoryChanged += Redraw;
            Redraw();
        }
    }

    // --- Drag ---

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
