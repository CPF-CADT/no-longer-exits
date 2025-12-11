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

    void Update()
    {
        if (lockMessageText != null && lockMessageText.enabled && Time.time > messageHideTime)
            lockMessageText.enabled = false;
    }

    public void ToggleDoor(ItemData itemInHand)
    {
        if (Time.time - lastInteractTime < 0.5f) return;
        lastInteractTime = Time.time;

        // LOCK CHECK
        if (requiredKey != null)
        {
            if (itemInHand == null || itemInHand != requiredKey)
            {
                ShowLockMessage();
                return;
            }
        }

        if (doorAnimator == null) return;

        isOpen = !isOpen;

        if (isOpen)
            doorAnimator.Play(openStateName, 0, 0f);
        else
            doorAnimator.Play(closeStateName, 0, 0f);
    }

    private void ShowLockMessage()
    {
        if (lockMessageText == null) return;

        lockMessageText.text = lockedMessage;
        lockMessageText.enabled = true;
        messageHideTime = Time.time + messageDuration;
    }

    public void OpenDoor()
    {
        if (doorAnimator == null || isOpen) return;
        isOpen = true;
        doorAnimator.Play(openStateName, 0, 0f);
    }

    public void CloseDoor()
    {
        if (doorAnimator == null || !isOpen) return;
        isOpen = false;
        doorAnimator.Play(closeStateName, 0, 0f);
    }
}
