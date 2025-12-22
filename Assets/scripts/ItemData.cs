using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName;
    public Sprite icon;
    public GameObject model;

    [Header("Spawn Transform")]
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;
    public Vector3 spawnScale = Vector3.one;

    [Header("Item Settings")]
    public bool isConsumable;
    public Sprite storyImage;   // Assign ONLY for story item

    [Header("System")]
    public string uniqueID;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
            SetDirty();
        }
    }

    [ContextMenu("Generate New ID")]
    private void GenerateNewID()
    {
        uniqueID = Guid.NewGuid().ToString();
        SetDirty();
    }

    private void SetDirty()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}
