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
            if (item != null && !itemDict.ContainsKey(item.uniqueID))
            {
                itemDict.Add(item.uniqueID, item);
            }
        }
    }

    public ItemData GetItemByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        itemDict.TryGetValue(id, out ItemData item);
        return item;
    }
}
