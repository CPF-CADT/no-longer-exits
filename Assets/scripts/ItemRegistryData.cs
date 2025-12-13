using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Registry/ItemRegistry")]
public class ItemRegistryData : ScriptableObject
{
    public ItemData[] items;

    public ItemData FindByName(string name)
    {
        if (string.IsNullOrEmpty(name) || items == null) return null;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            if (items[i].name == name || items[i].itemName == name) return items[i];
        }
        return null;
    }
}
