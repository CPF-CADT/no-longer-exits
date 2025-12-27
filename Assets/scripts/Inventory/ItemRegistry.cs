using System;
using UnityEngine;

public class ItemRegistry : MonoBehaviour
{
    public static ItemRegistry Instance;

    [Tooltip("Populate with all ItemData assets in the project (drag from Project window). This avoids placing assets in Resources.")]
    public ItemData[] items;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    public ItemData FindByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (items == null) return null;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            if (items[i].name == name || items[i].itemName == name) return items[i];
        }

        return null;
    }

    internal ItemData FindByUniqueID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (items == null) return null;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;

            if (items[i].UniqueID == id) return items[i];
        }

        return null;
    }

}
