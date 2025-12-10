using UnityEngine;
using System.Collections;

public class ChestController : MonoBehaviour
{
    [Header("Lock Settings")]
    public ItemData requiredKey;        // <--- DRAG YOUR KEY ITEM DATA HERE
    // (Leave empty if you want the chest to be unlocked)

    [Header("Loot Settings")]
    public GameObject itemToSpawn;      // Drag your Scroll Prefab here
    public Transform spawnPoint;        // Where the item appears
    public float spawnDelay = 0.5f;     

    [Header("Story Settings")]
    public Sprite storyImageForThisChest; 

    private Animation chestAnim;
    private bool isOpen = false;

    void Start()
    {
        chestAnim = GetComponentInChildren<Animation>();

        // Auto-find the "Item" spawn point if you haven't assigned it manually
        if (spawnPoint == null)
        {
            Transform foundItemLocation = transform.Find("Item");
            if (foundItemLocation != null) spawnPoint = foundItemLocation;
        }
    }

    // UPDATED: Now accepts the ItemData of what the player is holding
    public void OpenChest(ItemData itemInHand)
    {
        if (isOpen) return;

        // --- CHECK LOCK ---
        if (requiredKey != null)
        {
            // If the player is holding nothing, OR the wrong item
            if (itemInHand == null || itemInHand != requiredKey)
            {
                Debug.Log($"<color=red>LOCKED:</color> You need the {requiredKey.itemName} to open this!");
                return; // STOP HERE. Do not open.
            }
        }
        // ------------------

        if (chestAnim != null)
        {
            chestAnim.Play("ChestAnim");
            isOpen = true;

            // Move to "Ignore Raycast" so we can pick up the loot easily
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            StartCoroutine(SpawnItemRoutine());
        }
    }

    IEnumerator SpawnItemRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (itemToSpawn != null && spawnPoint != null)
        {
            // 1. Spawn the Item
            GameObject spawnedLoot = Instantiate(itemToSpawn, spawnPoint);
            spawnedLoot.transform.localPosition = Vector3.zero;
            spawnedLoot.transform.localRotation = Quaternion.identity;

            // 2. INJECT THE SPRITE
            ItemPickup pickup = spawnedLoot.GetComponent<ItemPickup>();
            
            if (pickup != null && storyImageForThisChest != null)
            {
                ItemData uniqueData = Instantiate(pickup.itemData);
                uniqueData.storyImage = storyImageForThisChest;
                pickup.itemData = uniqueData;
                
                Debug.Log($"Spawned Scroll with story: {storyImageForThisChest.name}");
            }
        }
    }
}