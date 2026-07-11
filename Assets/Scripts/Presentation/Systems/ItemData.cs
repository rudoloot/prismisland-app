using UnityEngine;
using System.Collections.Generic;
using PrismIsland.Data;

public static class ItemDatabase
{
    private static Dictionary<string, ItemDataSO> itemDict;

    public static void Initialize()
    {
        if (itemDict != null) return;
        itemDict = new Dictionary<string, ItemDataSO>();
        ItemDataSO[] items = Resources.LoadAll<ItemDataSO>("Items");
        foreach (var item in items) {
            if (!string.IsNullOrEmpty(item.id)) {
                itemDict[item.id] = item;
            }
        }
    }

    public static ItemDataSO GetItem(string id)
    {
        Initialize();
        if (itemDict.ContainsKey(id)) return itemDict[id];
        Debug.LogWarning("Item not found in Database: " + id);
        return null;
    }
}
