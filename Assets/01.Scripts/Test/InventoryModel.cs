using System;
using System.Collections.Generic;
using UnityEngine;
using Akasha;
using Akasha.Modifier;

[System.Serializable]
public class ItemData
{
    public int Id;
    public string Name;
    public int Quantity;
    public int MaxStack;
}

public class InventoryModel : BaseModel
{
    public RxVar<int> Gold { get; private set; }
    public RxVar<int> ItemCount { get; private set; }

    private Dictionary<int, ItemData> items = new Dictionary<int, ItemData>();

    public event Action<ItemData> ItemAdded;
    public event Action<int> ItemRemoved;

    public InventoryModel()
    {
        Gold = new RxVar<int>(1000, this);
        ItemCount = new RxVar<int>(0, this);
    }

    public void AddItem(ItemData newItem)
    {
        if (items.ContainsKey(newItem.Id))
        {
            var existingItem = items[newItem.Id];
            existingItem.Quantity = Mathf.Min(existingItem.Quantity + newItem.Quantity, existingItem.MaxStack);
        }
        else
        {
            items[newItem.Id] = newItem;
        }

        UpdateItemCount();
        ItemAdded?.Invoke(newItem);
    }

    public bool RemoveItem(int itemId, int quantity = 1)
    {
        if (!items.ContainsKey(itemId)) return false;

        var item = items[itemId];
        item.Quantity -= quantity;

        if (item.Quantity <= 0)
        {
            items.Remove(itemId);
            ItemRemoved?.Invoke(itemId);
        }

        UpdateItemCount();
        return true;
    }

    public void AddGold(int amount)
    {
        Gold.Set(Gold.Value + amount);
    }

    public bool SpendGold(int amount)
    {
        if (Gold.Value >= amount)
        {
            Gold.Set(Gold.Value - amount);
            return true;
        }
        return false;
    }

    public IEnumerable<ItemData> GetAllItems()
    {
        return items.Values;
    }

    private void UpdateItemCount()
    {
        ItemCount.Set(items.Count);
    }
}