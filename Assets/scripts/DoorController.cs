using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Assign Reference")]
    public Animator doorAnimator; // Drag the Child (TimsAssets_Door) here

    [Header("Settings")]
    public string animParameter = "open";

    private bool isOpen = false;

    // Called by Player (Pressing E)
    public void ToggleDoor()
    {
        if (doorAnimator != null)
        {
            isOpen = !isOpen;
            doorAnimator.SetBool(animParameter, isOpen);
        }
    }

    // NEW: Called by Ghost (Auto-open only)
    public void OpenDoor()
    {
        // Only open if it is currently closed
        if (!isOpen && doorAnimator != null)
        {
            isOpen = true;
            doorAnimator.SetBool(animParameter, true);
            Debug.Log("Ghost opened the door!");
        }
    }
}