using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Assign Reference")]
    public Animator doorAnimator; // Drag the Child (TimsAssets_Door) here

    [Header("Settings")]
    public string animParameter = "open";

    private bool isOpen = false;

    // This function is called by the Player's Raycast script
    public void ToggleDoor()
    {
        if (doorAnimator != null)
        {
            isOpen = !isOpen;
            doorAnimator.SetBool(animParameter, isOpen);
            Debug.Log("Door Toggled: " + isOpen);
        }
    }
}