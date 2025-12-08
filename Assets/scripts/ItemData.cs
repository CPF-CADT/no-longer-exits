using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;        
    public GameObject model;  

    [Header("Hand Positioning")]
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;
}