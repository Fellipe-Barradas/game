using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

[System.Serializable]
public class InventoryEntry
{
    public ItemSO item;
    public int amount;
}

public class Inventory : MonoBehaviour
{
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask pickupMask = ~0;
    [SerializeField] private Material highLightMaterial;
    [SerializeField] private float equipedOpacity = 0.9f;
    [SerializeField] private float normalOpacity = 0.58f;
    private int equippedHotbarIndex = 0;
    private int hotbarStartIndex;
    private int hotbarSize;
    private Item lookedAtItem = null;
    private Material originalMaterial;
    private Renderer lookedAtRenderer = null;
    private Collider[] selfColliders;
    private string lastRaycastLog = "";

    private List<InventoryEntry> entries = new();

    public IReadOnlyList<InventoryEntry> Entries => entries;
    public int Size => entries.Count;

    public event Action OnInventoryChanged;
    public event Action OnEquippedChanged;

    public int EquippedHotbarIndex => equippedHotbarIndex;
    public int HotbarStartIndex => hotbarStartIndex;
    public float EquipedOpacity => equipedOpacity;
    public float NormalOpacity => normalOpacity;

    public void SetHotbarInfo(int startIndex, int size)
    {
        hotbarStartIndex = startIndex;
        hotbarSize = size;
    }

    public void Initialize(int slotCount)
    {
        entries.Clear();
        for (int i = 0; i < slotCount; i++)
            entries.Add(new InventoryEntry { item = null, amount = 0 });
    }
    
    private void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.CanPlayerAct) return;
        DetectLookedAtItem();
        Pickup();
        HandleHotbarSelection();
        HandleDropEquippedItem();
    }
    
    public void AddItem(ItemSO itemToAdd, int amount)
    {
        int remaining = amount;
        
        foreach (var entry in entries)
        {
            if (entry.item == itemToAdd && entry.amount < itemToAdd.maxStackSize)
            {
                int space = itemToAdd.maxStackSize - entry.amount;
                int toAdd = Mathf.Min(space, remaining);
                entry.amount += toAdd;
                remaining -= toAdd;
                if (remaining <= 0) break;
            }
        }
        
        if (remaining > 0)
        {
            foreach (var entry in entries)
            {
                if (entry.item == null)
                {
                    int toPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                    entry.item = itemToAdd;
                    entry.amount = toPlace;
                    remaining -= toPlace;
                    if (remaining <= 0) break;
                }
            }
        }
        
        if (remaining > 0)
            Debug.Log($"Inventário cheio, sobraram {remaining} de {itemToAdd.itemName}");
        
        OnInventoryChanged?.Invoke();
    }
    
    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (fromIndex < 0 || fromIndex >= entries.Count) return;
        if (toIndex < 0 || toIndex >= entries.Count) return;

        var from = entries[fromIndex];
        var to = entries[toIndex];

        if (to.item == from.item && to.item != null)
        {
            int space = to.item.maxStackSize - to.amount;
            int transfer = Mathf.Min(space, from.amount);
            to.amount += transfer;
            from.amount -= transfer;
            if (from.amount <= 0) { from.item = null; from.amount = 0; }
        }
        else
        {
            (entries[fromIndex], entries[toIndex]) = (entries[toIndex], entries[fromIndex]);
        }

        OnInventoryChanged?.Invoke();
    }

    public void RemoveItem(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= entries.Count) return;
        var entry = entries[slotIndex];
        if (entry.item == null) return;
        
        entry.amount -= amount;
        if (entry.amount <= 0)
        {
            entry.item = null;
            entry.amount = 0;
        }
        OnInventoryChanged?.Invoke();
    }

    private void HandleHotbarSelection()
    {
        if (hotbarSize == 0) return;

        var kb = Keyboard.current;
        Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
        for (int i = 0; i < Mathf.Min(digits.Length, hotbarSize); i++)
        {
            if (kb[digits[i]].wasPressedThisFrame)
            {
                equippedHotbarIndex = i;
                OnEquippedChanged?.Invoke();
                return;
            }
        }

        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            equippedHotbarIndex += scroll > 0f ? -1 : 1;
            equippedHotbarIndex = (equippedHotbarIndex % hotbarSize + hotbarSize) % hotbarSize;
            OnEquippedChanged?.Invoke();
        }
    }

    private void HandleDropEquippedItem()
    {
        if (!Keyboard.current.qKey.wasPressedThisFrame) return;

        int slotIndex = hotbarStartIndex + equippedHotbarIndex;
        if (slotIndex >= entries.Count) return;

        var entry = entries[slotIndex];
        if (entry.item == null) return;

        GameObject prefab = entry.item.itemPrefab;
        if (prefab == null) return;

        Vector3 dropPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
        GameObject dropped = Instantiate(prefab, dropPos, Quaternion.identity);

        Item itemComponent = dropped.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.item = entry.item;
            itemComponent.amount = entry.amount;
        }

        RemoveItem(slotIndex, entry.amount);
    }

    private void Pickup()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (lookedAtItem == null) return;
            AddItem(lookedAtItem.item, lookedAtItem.amount);
            Destroy(lookedAtItem.gameObject);
            lookedAtItem = null;
            lookedAtRenderer = null;
            originalMaterial = null;
        }
    }

    private void DetectLookedAtItem()
    {
        string currentLog = "";

        // LIMPEZA SEGURA: Evita o MissingReferenceException
        if (lookedAtRenderer != null)
        {
            try {
                lookedAtRenderer.material = originalMaterial;
            } catch { }
        }

        lookedAtRenderer = null;
        originalMaterial = null;
        lookedAtItem = null;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        float maxDist = pickupRange + 2f; 
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDist, pickupMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == null || Array.IndexOf(selfColliders, hit.collider) >= 0)
                continue;

            if (Vector3.Distance(transform.position, hit.point) > pickupRange)
            {
                currentLog = $"[Inventory] Hit '{hit.collider.name}' fora do alcance.";
                break;
            }

            Item item = hit.collider.GetComponentInParent<Item>();
            if (item == null)
            {
                currentLog = $"[Inventory] Hit '{hit.collider.name}' — sem Item.";
                break;
            }

            Renderer rend = item.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                originalMaterial = rend.material;
                rend.material = highLightMaterial;
                lookedAtRenderer = rend;
                lookedAtItem = item;
                currentLog = $"[Inventory] Destacando: '{item.name}'";
            }
            break;
        }

        if (currentLog != lastRaycastLog)
        {
            if (currentLog == "") currentLog = "[Inventory] Nada em foco.";
            Debug.Log(currentLog);
            lastRaycastLog = currentLog;
        }
    }
}