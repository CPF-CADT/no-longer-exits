using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Lock Settings")]
    public ItemData requiredKey; // Drag Key ItemData here. Leave EMPTY to unlock by default.

    [Header("Assign Reference")]
    public Animator doorAnimator; 

    [Header("State Names")]
    public string openStateName = "Door_open";
    public string closeStateName = "Door_Close";

    private bool isOpen = false;
    private float lastInteractTime = 0f; 

    // UPDATE: We now ask "What item are you holding?"
    public void ToggleDoor(ItemData itemInHand)
    {
        // 1. Cooldown Check
        if (Time.time - lastInteractTime < 0.5f) return; 
        lastInteractTime = Time.time;

        // 2. LOCK CHECK
        // Only check if a key is actually assigned in the Inspector
        if (requiredKey != null)
        {
            // If hand is empty OR holding the wrong item
            if (itemInHand == null || itemInHand != requiredKey)
            {
                Debug.Log($"<color=red>LOCKED:</color> You need the {requiredKey.itemName} to open this door!");
                return; // STOP HERE. Do not open.
            }
        }

        // 3. Normal Door Logic
        if (doorAnimator == null) return;

        isOpen = !isOpen;

        if (isOpen)
        {
            doorAnimator.Play(openStateName, 0, 0.0f);
            Debug.Log("Playing Open Animation Direct");
        }
        else
        {
            doorAnimator.Play(closeStateName, 0, 0.0f);
            Debug.Log("Playing Close Animation Direct");
        }
    }

    // Keep these for ghosts/scripts (they bypass the key check)
    public void OpenDoor()
    {
        if (doorAnimator == null || isOpen) return;
        isOpen = true;
        doorAnimator.Play(openStateName, 0, 0.0f);
    }

    public void CloseDoor()
    {
        if (doorAnimator == null || !isOpen) return;
        isOpen = false;
        doorAnimator.Play(closeStateName, 0, 0.0f);
    }
}