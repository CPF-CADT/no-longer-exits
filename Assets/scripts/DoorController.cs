using UnityEngine;
using TMPro;

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

    private bool isOpen = false;
    private float lastInteractTime = 0f;
    private float messageHideTime = 0f;

    public bool lockedByPuzzle = false;
    
    [Header("Persistence")]
    public PersistentID persistentID;

    private void Awake()
    {
        // 1. Try to find the component if not manually assigned
        if (persistentID == null)
            persistentID = GetComponent<PersistentID>();

        // 2. ERROR CHECK: If still missing, warn the user.
        // We do NOT use AddComponent here to avoid random ID generation.
        if (persistentID == null)
        {
            Debug.LogError($"[DoorController] '{name}' is missing a PersistentID component! Save/Load will fail.");
        }
    }

    void Update()
    {
        if (lockMessageText != null && lockMessageText.enabled && Time.time > messageHideTime)
            lockMessageText.enabled = false;
    }

    /// <summary>
    /// Call this when the player interacts with the door
    /// </summary>
    public void ToggleDoor(ItemData itemInHand)
    {
        if (lockedByPuzzle) return; 

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
        // Play animation from start (0f) for normal interaction
        doorAnimator.Play(isOpen ? openStateName : closeStateName, 0, 0f);
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
            
            // FIX: Snap the animation to the END (1.0f) so it doesn't replay the motion on load.
            doorAnimator.Play(animToPlay, 0, 1.0f); 
            
            // Force update so the visual snaps immediately
            doorAnimator.Update(0f);
        }
    }
}