using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public int totalWeapons = 4;
    private int placedCorrectWeapons = 0;

    [Header("Optional")]
    public DoorController doorController;    // assign your door here
    public Animator statueAnimator;

    public void NotifyWeaponPlaced()
    {
        placedCorrectWeapons++;
        Debug.Log($"Correct weapons placed: {placedCorrectWeapons}/{totalWeapons}");

        if (placedCorrectWeapons >= totalWeapons)
            PuzzleCompleted();
    }

    public void NotifyWeaponRemoved()
    {
        placedCorrectWeapons--;
        if (placedCorrectWeapons < 0) placedCorrectWeapons = 0;

        Debug.Log($"Correct weapons removed. Current: {placedCorrectWeapons}/{totalWeapons}");
    }

    private void PuzzleCompleted()
    {
        Debug.Log("Vishnu Puzzle Completed!");

        // Open the door via DoorController
        if (doorController != null)
        {
            doorController.lockedByPuzzle = false;  // unlock door
            doorController.OpenDoor();              // open door automatically
        }
        // Optional: trigger statue animation
        if (statueAnimator != null)
            statueAnimator.SetTrigger("Awaken");
    }
}
