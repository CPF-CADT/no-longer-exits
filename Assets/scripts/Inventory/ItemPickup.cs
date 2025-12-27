using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemData; // Drag the Item Data here

    // Internal claimed flag to avoid duplicate pickups
    private bool isClaimed = false;

    // Attempt to claim this pickup for one caller. Returns true if caller may pick it up.
    public bool TryClaim()
    {
        if (isClaimed) return false;
        isClaimed = true;

        // Disable collider so further raycasts won't hit it
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        return true;
    }

    // This function is called by the Player when they press E
    public void Pickup()
    {
        Destroy(gameObject); // Object disappears from floor
    }
}