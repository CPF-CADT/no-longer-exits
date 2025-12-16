#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject model;
    public Vector3 spawnPosition, spawnRotation, spawnScale;
    public bool isConsumable;
    public Sprite storyImage;

    [HideInInspector]
    public string uniqueID;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = Guid.NewGuid().ToString();
        EditorUtility.SetDirty(this);
    }
#endif
}
