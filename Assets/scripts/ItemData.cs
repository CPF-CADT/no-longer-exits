using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject model;
    public Vector3 spawnPosition, spawnRotation, spawnScale;
    public bool isConsumable;
    public Sprite storyImage;

    [Header("System")]
    [Tooltip("Auto-generated unique ID. Do not change manually.")]
    public string uniqueID;

    private void OnValidate()
    {
        // Automatically generate ID if it is missing
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

    private new void SetDirty()
    {
#if UNITY_EDITOR
        // This tells Unity "The data changed, please save this to the disk"
        EditorUtility.SetDirty(this);
#endif
    }
}