using UnityEngine;
using System.Collections;

public class ChestController : MonoBehaviour
{
    [Header("Loot Settings")]
    public GameObject itemToSpawn;      // Drag your Scroll Prefab here
    public Transform spawnPoint;        // Where the item appears (usually a child object inside the chest)
    public float spawnDelay = 0.5f;     // Delay before the item appears

    [Header("Story Settings")]
    public Sprite storyImageForThisChest; // <--- DRAG YOUR UNIQUE STORY IMAGE HERE

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

    // Called by PlayerInteract
    public void OpenChest()
    {
        if (isOpen) return;

        if (chestAnim != null)
        {
            chestAnim.Play("ChestAnim");
            isOpen = true;

            // Move the chest to the "Ignore Raycast" layer 
            // This prevents the chest collider from blocking your click on the item inside
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
            // Find the Pickup script on the newly spawned object
            ItemPickup pickup = spawnedLoot.GetComponent<ItemPickup>();
            
            if (pickup != null && storyImageForThisChest != null)
            {
                // CRITICAL STEP: Create a UNIQUE CLONE of the ItemData.
                // If we don't do this, changing the image will change it for EVERY scroll in the game!
                ItemData uniqueData = Instantiate(pickup.itemData);
                
                // Set the specific image on this unique copy
                uniqueData.storyImage = storyImageForThisChest;
                
                // Assign the modified data back to the pickup item
                pickup.itemData = uniqueData;
                
                Debug.Log($"Spawned Scroll with story: {storyImageForThisChest.name}");
            }
        }
    }
}