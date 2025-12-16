using UnityEngine;
using TMPro;

public class DoorController : MonoBehaviour
{
    [Header("Lock Settings")]
    public ItemData requiredKey;

    [Header("Assign Reference")]
    public Animator doorAnimator;

    [Header("UI")]
    public TextMeshProUGUI lockMessageText;   // Drag your TMP UI here
    public string lockedMessage = "The door is locked."; // Customizable message
    public float messageDuration = 2f;

    [Header("State Names")]
    public string openStateName = "Door_open";
    public string closeStateName = "Door_Close";

    private bool isOpen = false;
    private float lastInteractTime = 0f;
    private float messageHideTime = 0f;

    public bool lockedByPuzzle = false;

    void Update()
    {
        if (lockMessageText != null && lockMessageText.enabled && Time.time > messageHideTime)
            lockMessageText.enabled = false;
    }

    /// <summary>
    /// Call this when the player interacts with the door
    /// </summary>
    /// <param name="itemInHand">The currently selected item from InventorySystem</param>
    public void ToggleDoor(ItemData itemInHand)
    {
        if (lockedByPuzzle) return;  // cannot open manually if locked by puzzle

        if (Time.time - lastInteractTime < 0.5f) return;
        lastInteractTime = Time.time;

        // --- KEY CHECK: compare by uniqueID ---
        if (requiredKey != null)
        {
            if (itemInHand == null || itemInHand.uniqueID != requiredKey.uniqueID)
            {
                ShowLockMessage();
                return;
            }
        }

        if (doorAnimator == null) return;

        isOpen = !isOpen;
        doorAnimator.Play(isOpen ? openStateName : closeStateName, 0, 0f);
    }

    private void ShowLockMessage()
    {
        if (lockMessageText == null) return;

        lockMessageText.text = lockedMessage;
        lockMessageText.enabled = true;
        messageHideTime = Time.time + messageDuration;
    }

    /// <summary>
    /// Opens the door without needing a key
    /// </summary>
    public void OpenDoor()
    {
        if (doorAnimator == null || isOpen) return;
        isOpen = true;
        doorAnimator.Play(openStateName, 0, 0f);
    }

    /// <summary>
    /// Closes the door
    /// </summary>
    public void CloseDoor()
    {
        if (doorAnimator == null || !isOpen) return;
        isOpen = false;
        doorAnimator.Play(closeStateName, 0, 0f);
    }
}
