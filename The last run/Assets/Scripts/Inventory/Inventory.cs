using UnityEngine;
using System.Collections.Generic;
using System;

public class Inventory : MonoBehaviour
{
    [SerializeField] private List<Item> items = new List<Item>();
    public int maxSlots = 3;

    public event Action InventoryChanged;

    public bool AddItem(Item item)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("Inventory is full!");
            return false;
        }
        Debug.Log($"there is {maxSlots - items.Count} slots left in the inventory");
        items.Add(item);
        item.OnPickup(this);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(Item item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            item.OnDrop();
            InventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public List<Item> GetAllItems()
    {
        return new List<Item>(items);
    }

    public int GetItemCount()
    {
        return items.Count;
    }

    public bool HasItem(string itemName)
    {
        return items.Exists(item => item.itemName == itemName);
    }

    public Item GetItem(string itemName)
    {
        return items.Find(item => item.itemName == itemName);
    }
}
