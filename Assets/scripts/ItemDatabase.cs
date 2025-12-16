using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public ItemData[] allItems;

    private Dictionary<string, ItemData> itemDict;

    private void OnEnable()
    {
        itemDict = new Dictionary<string, ItemData>();
        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (string.IsNullOrEmpty(item.uniqueID))
            {
                Debug.LogWarning($"ItemDatabase: Item '{item.name}' has empty uniqueID and will be skipped. Assign a manual ID.");
                continue;
            }

            if (itemDict.ContainsKey(item.uniqueID))
            {
                Debug.LogWarning($"ItemDatabase: Duplicate uniqueID '{item.uniqueID}' found for item '{item.name}'. Only the first will be used.");
                continue;
            }

            itemDict.Add(item.uniqueID, item);
        }
    }

    public ItemData GetItemByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        itemDict.TryGetValue(id, out ItemData item);
        return item;
    }
}
