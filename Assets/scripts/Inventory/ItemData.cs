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
    [Tooltip("Assign ONLY for story / scroll items")]
    public Sprite storyImage;

    [Header("System (DO NOT CHANGE AFTER RELEASE)")]
    [SerializeField] public string UniqueID;

    // --------------------------------------------------
    // ID SAFETY
    // --------------------------------------------------

    private void OnEnable()
    {
        // Runtime safety (build + editor)
        EnsureID();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Editor safety
        EnsureID();
    }
#endif

    private void EnsureID()
    {
        if (string.IsNullOrEmpty(UniqueID))
        {
            UniqueID = Guid.NewGuid().ToString();
            MarkDirty();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("⚠ Generate NEW ID (BREAKS SAVES)")]
    private void GenerateNewID()
    {
        if (!EditorUtility.DisplayDialog(
            "Generate New ID?",
            "This will BREAK all existing save files using this item.\n\nAre you sure?",
            "Yes, Break Saves",
            "Cancel"))
            return;

        UniqueID = Guid.NewGuid().ToString();
        MarkDirty();
    }
#endif

    private void MarkDirty()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
#endif
    }
}
