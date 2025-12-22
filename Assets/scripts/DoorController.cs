using UnityEngine;
using TMPro;
using System.Collections;

public class DoorController : MonoBehaviour, ISaveable
{
    [Header("Lock Settings")]
    public ItemData requiredKey;

    [Header("Assign Reference")]
    public Animator doorAnimator;

    [Header("UI")]
    public TextMeshProUGUI lockMessageText;
    public string lockedMessage = "The door is locked.";
    public float messageDuration = 2f;

    [Header("State Names")]
    public string openStateName = "Door_open";
    public string closeStateName = "Door_Close";

    private InteractionHint hint;
    public bool isOpen = false;
    private float lastInteractTime = 0f;
    private float messageHideTime = 0f;
    public bool lockedByPuzzle = false;

    [Header("Persistence")]
    public PersistentID persistentID;

    private void Awake()
    {
        if (persistentID == null)
            persistentID = GetComponent<PersistentID>();

        if (persistentID == null)
            Debug.LogError($"[DoorController] '{name}' is missing a PersistentID component! Save/Load will fail.");

        hint = GetComponentInChildren<InteractionHint>(true);

        if (hint == null)
            Debug.LogError($"{name} is missing InteractionHint component");

        // Make sure the lock message is hidden at start
        if (lockMessageText != null)
            lockMessageText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (hint != null)
            hint.useAlternate = isOpen;

        // Hide lock message after duration
        if (lockMessageText != null && lockMessageText.gameObject.activeSelf && Time.time > messageHideTime)
            lockMessageText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Call this when the player interacts with the door
    /// </summary>
    public void ToggleDoor(ItemData itemInHand)
    {
        if (lockedByPuzzle) return;

        // Prevent rapid toggling
        if (Time.time - lastInteractTime < 0.5f) return;
        lastInteractTime = Time.time;

        // --- KEY CHECK ---
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
        hint.useAlternate = isOpen;
    }

    private void ShowLockMessage()
    {
        if (lockMessageText == null)
        {
            Debug.LogWarning("lockMessageText not assigned!");
            return;
        }

        lockMessageText.text = lockedMessage;
        lockMessageText.gameObject.SetActive(true);
        messageHideTime = Time.time + messageDuration;

        Debug.Log("Lock message shown: " + lockedMessage);
    }


    public void OpenDoor()
    {
        if (doorAnimator == null || isOpen) return;

        isOpen = true;
        doorAnimator.Play(openStateName, 0, 0f);
        hint.useAlternate = true;
    }

    public void CloseDoor()
    {
        if (doorAnimator == null || !isOpen) return;

        isOpen = false;
        doorAnimator.Play(closeStateName, 0, 0f);
        hint.useAlternate = false;
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
            type = "Door",
            doorOpen = isOpen,
            doorLockedByPuzzle = lockedByPuzzle
        };
    }

    public void RestoreState(SaveObjectState state)
    {
        if (state == null) return;
        if (state.type != null && state.type != "Door") return;

        lockedByPuzzle = state.doorLockedByPuzzle;
        isOpen = state.doorOpen;

        if (doorAnimator != null)
        {
            string animToPlay = isOpen ? openStateName : closeStateName;
            doorAnimator.Play(animToPlay, 0, 1.0f);
            doorAnimator.Update(0f);
        }

        // Ensure hint state is correct after load
        if (hint != null)
            hint.useAlternate = isOpen;
    }
}
