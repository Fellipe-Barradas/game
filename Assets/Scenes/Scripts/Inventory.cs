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
    [SerializeField] private ItemSO initialSword;

    private List<InventoryEntry> entries = new();

    public IReadOnlyList<InventoryEntry> Entries => entries;
    public int Size => entries.Count;

    public event Action OnInventoryChanged;

    public void Initialize(int slotCount)
    {
        entries.Clear();
        for (int i = 0; i < slotCount; i++)
            entries.Add(new InventoryEntry { item = null, amount = 0 });
    }
    
    private void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
            AddItem(initialSword, 1);
    }
    
    public void AddItem(ItemSO itemToAdd, int amount)
    {
        int remaining = amount;
        
        // 1. tenta empilhar em slots existentes
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
        
        // 2. coloca em slots vazios
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
}