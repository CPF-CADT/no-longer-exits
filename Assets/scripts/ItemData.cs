using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject model;

    [Header("Story Settings")]
    public Sprite storyImage; // For readable/scroll items

    [Header("Hand Positioning")]
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;
    public Vector3 spawnScale = Vector3.one; // <-- New: scale in hand

    [Tooltip("Destroy this item after use?")]
    public bool isConsumable = false;
}
