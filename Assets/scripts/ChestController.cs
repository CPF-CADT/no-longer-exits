using UnityEngine;
using System.Collections;

public class ChestController : MonoBehaviour
{
    [Header("Loot Settings")]
    public GameObject itemToSpawn;      
    public Transform spawnPoint;        // This will be your "Item" object
    public float spawnDelay = 0.5f;     

    private Animation chestAnim;
    private bool isOpen = false;

    void Start()
    {
        chestAnim = GetComponentInChildren<Animation>();

        // Auto-find the "Item" object if you haven't assigned it manually
        if (spawnPoint == null)
        {
            Transform foundItemLocation = transform.Find("Item");
            if (foundItemLocation != null)
            {
                spawnPoint = foundItemLocation;
            }
        }
    }

    public void OpenChest()
    {
        if (isOpen) return;

        if (chestAnim != null)
        {
            chestAnim.Play("ChestAnim");
            isOpen = true;

            // --- NEW CODE: STOP BLOCKING THE RAY ---
            // This moves the chest to the "Ignore Raycast" layer (Layer 2)
            // The player can still walk into it, but the interact ray will pass through!
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            // ---------------------------------------

            StartCoroutine(SpawnItemRoutine());
        }
    }

    IEnumerator SpawnItemRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (itemToSpawn != null && spawnPoint != null)
        {
            // 1. Spawn the object as a CHILD of 'spawnPoint' (the Item object)
            GameObject spawnedLoot = Instantiate(itemToSpawn, spawnPoint);

            // 2. Force the position to be exactly 0,0,0 inside that parent
            spawnedLoot.transform.localPosition = Vector3.zero;
            spawnedLoot.transform.localRotation = Quaternion.identity; // Align rotation too

            Debug.Log("Item spawned inside 'Item' container at 0,0,0!");
        }
    }
}