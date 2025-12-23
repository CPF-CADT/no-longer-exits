using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public ItemData[] allItems;

    // The Dictionary is not serialized, so it becomes null on reload
    private Dictionary<string, ItemData> itemDict;

    public void Init()
    {
        // --- FIX: Check if the Dictionary is null, not just a boolean flag ---
        if (itemDict != null && itemDict.Count > 0) return;

        itemDict = new Dictionary<string, ItemData>();

        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (string.IsNullOrEmpty(item.UniqueID))
            {
                Debug.LogError($"Item '{item.name}' has EMPTY uniqueID");
                continue;
            }

            if (itemDict.ContainsKey(item.UniqueID))
            {
                Debug.LogError($"DUPLICATE uniqueID: {item.UniqueID}");
                continue;
            }

            itemDict.Add(item.UniqueID, item);
        }
    }

    public ItemData GetItemByID(string id)
    {
        Init(); // Ensure we are initialized

        if (string.IsNullOrEmpty(id)) return null;
        
        // Safety Check
        if (itemDict == null) return null;

        itemDict.TryGetValue(id, out ItemData item);
        return item;
    }
}