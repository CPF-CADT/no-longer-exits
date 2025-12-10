using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Assign Reference")]
    public Animator doorAnimator; 

    [Header("State Names")]
    public string openStateName = "Door_open";
    public string closeStateName = "Door_Close";

    private bool isOpen = false;
    private float lastInteractTime = 0f; // Stores the time of the last click

    public void ToggleDoor()
    {
        // --- COOLDOWN FIX ---
        // If less than 0.5 seconds have passed since the last click, IGNORE this click.
        if (Time.time - lastInteractTime < 0.5f) return; 
        lastInteractTime = Time.time;
        // --------------------

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

    // Optional: Add cooldowns to these too if needed
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