using UnityEngine;
using System.Collections;
using TMPro;

public class ChestController : MonoBehaviour
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
    public TextMeshProUGUI lockMessageText;  // Drag UI Text here
    public string lockedMessage = "The chest is locked.";
    public float messageDuration = 2f;

    private Animation chestAnim;
    private bool isOpen = false;
    private float messageHideTime = 0f;

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

        // LOCK CHECK
        if (requiredKey != null)
        {
            if (itemInHand == null || itemInHand != requiredKey)
            {
                ShowLockMessage();
                return;
            }
        }

        if (chestAnim != null)
        {
            chestAnim.Play("ChestAnim");
            isOpen = true;

            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            StartCoroutine(SpawnItemRoutine());
        }
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

        if (itemToSpawn != null && spawnPoint != null)
        {
            GameObject spawnedLoot = Instantiate(itemToSpawn, spawnPoint);
            spawnedLoot.transform.localPosition = Vector3.zero;
            spawnedLoot.transform.localRotation = Quaternion.identity;

            ItemPickup pickup = spawnedLoot.GetComponent<ItemPickup>();

            if (pickup != null && storyImageForThisChest != null)
            {
                ItemData uniqueData = Instantiate(pickup.itemData);
                uniqueData.storyImage = storyImageForThisChest;
                pickup.itemData = uniqueData;
            }
        }
    }
}
