using UnityEngine;
using System.Collections;
using TMPro;

public class ChestController : MonoBehaviour, ISaveable
{
    [Header("Lock Settings")]
    public ItemData requiredKey;

    [Header("Loot Settings")]
    public GameObject itemToSpawn;
    public Transform spawnPoint;
    public float spawnDelay = 0.5f;

    [Header("Story Settings")]
    public Sprite storyImageForThisChest;

    [Header("UI")]
    public TextMeshProUGUI lockMessageText;
    public string lockedMessage = "The chest is locked.";
    public float messageDuration = 2f;

    private Animation chestAnim;
    private bool isOpen = false;
    private float messageHideTime = 0f;

    [Header("Persistence")]
    public PersistentID persistentID;

    // specific variable to track if loot has ever been generated
    [SerializeField] private bool hasSpawned = false;

    private void Awake()
    {
        if (persistentID == null)
            persistentID = GetComponent<PersistentID>();

        if (persistentID == null)
        {
            Debug.LogError($"[ChestController] '{name}' is missing a PersistentID component! Fix this in the Inspector.");
        }
    }

    void Start()
    {
        chestAnim = GetComponentInChildren<Animation>();

        if (spawnPoint == null)
        {
            Transform foundItemLocation = transform.Find("Item");
            if (foundItemLocation != null)
                spawnPoint = foundItemLocation;
        }
    }

    void Update()
    {
        if (lockMessageText != null && lockMessageText.enabled && Time.time > messageHideTime)
            lockMessageText.enabled = false;
    }

    public void OpenChest(ItemData itemInHand)
    {
        if (isOpen) return;

        // --- LOCK CHECK ---
        if (requiredKey != null)
        {
            if (itemInHand == null || itemInHand.uniqueID != requiredKey.uniqueID)
            {
                ShowLockMessage();
                return;
            }
        }

        PerformOpen();
    }

    private void PerformOpen()
    {
        isOpen = true;

        if (chestAnim != null)
            chestAnim.Play("ChestAnim");

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Only spawn if we haven't done so before
        if (!hasSpawned)
            StartCoroutine(SpawnItemRoutine());
    }

    private void ShowLockMessage()
    {
        if (lockMessageText == null) return;
        lockMessageText.text = lockedMessage;
        lockMessageText.enabled = true;
        messageHideTime = Time.time + messageDuration;
    }

    IEnumerator SpawnItemRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnLootObject();
    }

    private void SpawnLootObject()
    {
        if (itemToSpawn != null && spawnPoint != null)
        {
            GameObject spawnedLoot = Instantiate(itemToSpawn, spawnPoint);
            spawnedLoot.transform.localPosition = Vector3.zero;
            spawnedLoot.transform.localRotation = Quaternion.identity;

            // Mark as spawned immediately so we never do it again
            hasSpawned = true;

            ItemPickup pickup = spawnedLoot.GetComponent<ItemPickup>();
            if (pickup != null && storyImageForThisChest != null)
            {
                ItemData uniqueData = Instantiate(pickup.itemData);
                uniqueData.storyImage = storyImageForThisChest;
                pickup.itemData = uniqueData;
            }
        }
    }

    // -------------------- ISaveable --------------------
    public string GetUniqueID()
    {
        return persistentID != null ? persistentID.id : "";
    }

    public SaveObjectState CaptureState()
    {
        return new SaveObjectState
        {
            id = GetUniqueID(),
            type = "Chest",
            chestOpen = isOpen,
            chestSpawned = hasSpawned
        };
    }

    public void RestoreState(SaveObjectState state)
    {
        if (state == null || state.type != "Chest") return;

        isOpen = state.chestOpen;
        hasSpawned = state.chestSpawned;

        // Visual Restore
        if (isOpen)
        {
            // CASE 1: The chest was saved as OPEN
            if (chestAnim != null)
            {
                chestAnim.Play("ChestAnim");
                // Fast forward animation to the end so it doesn't play the opening sequence again
                foreach (AnimationState animState in chestAnim)
                {
                    animState.normalizedTime = 1.0f; 
                }
                chestAnim.Sample();
            }

            // Disable interaction
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        else
        {
            // CASE 2: The chest was saved as CLOSED
            if (chestAnim != null)
            {
                chestAnim.Stop();
                // Reset animation to start
                foreach (AnimationState animState in chestAnim)
                {
                    animState.normalizedTime = 0.0f;
                }
                chestAnim.Sample();
            }

            // Enable interaction
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }
}