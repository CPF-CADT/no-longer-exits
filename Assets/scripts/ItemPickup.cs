using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemData; // Drag the Item Data here

    // This function is called by the Player when they press E
    public void Pickup()
    {
        Destroy(gameObject); // Object disappears from floor
    }
}