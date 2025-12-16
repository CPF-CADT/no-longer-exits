using UnityEngine;

public class PuzzleManager : MonoBehaviour, ISaveable
{
    [Header("Settings")]
    public int totalWeapons = 4;
    [SerializeField] private int placedCorrectWeapons = 0; 

    [Header("References")]
    public DoorController doorController;
    public Animator statueAnimator;

    [Header("Persistence")]
    public PersistentID persistentID;

    private void Awake()
    {
        if (persistentID == null)
            persistentID = GetComponent<PersistentID>();

        // CRITICAL: We do NOT auto-add the component anymore.
        // If this error appears, you must add 'PersistentID' in the Inspector.
        if (persistentID == null)
            Debug.LogError($"[PuzzleManager] ERROR: '{name}' has no PersistentID! Save/Load will fail.");
    }

    public void NotifyWeaponPlaced()
    {
        placedCorrectWeapons++;
        if (placedCorrectWeapons > totalWeapons) placedCorrectWeapons = totalWeapons;
        
        Debug.Log($"[PuzzleManager] Progress: {placedCorrectWeapons}/{totalWeapons}");

        if (placedCorrectWeapons >= totalWeapons) PuzzleCompleted();
    }

    public void NotifyWeaponRemoved()
    {
        placedCorrectWeapons--;
        if (placedCorrectWeapons < 0) placedCorrectWeapons = 0;
    }

    private void PuzzleCompleted()
    {
        Debug.Log("[PuzzleManager] PUZZLE SOLVED!");
        if (doorController != null)
        {
            doorController.lockedByPuzzle = false;
            doorController.OpenDoor();
        }
        if (statueAnimator != null) statueAnimator.SetTrigger("Awaken");
    }

    // --- SAVE/LOAD LOGIC ---
    public string GetUniqueID()
    {
        return persistentID != null ? persistentID.id : "";
    }

    public SaveObjectState CaptureState()
    {
        return new SaveObjectState
        {
            id = GetUniqueID(),
            type = "Puzzle",
            puzzlePlacedCorrect = placedCorrectWeapons
        };
    }

    public void RestoreState(SaveObjectState state)
    {
        if (state == null || state.type != "Puzzle") return;

        placedCorrectWeapons = state.puzzlePlacedCorrect;
        Debug.Log($"[PuzzleManager] LOADED Count: {placedCorrectWeapons}/{totalWeapons}");

        // Restore door state based on loaded count
        if (placedCorrectWeapons >= totalWeapons)
        {
            if (doorController != null) { doorController.lockedByPuzzle = false; doorController.OpenDoor(); }
            if (statueAnimator != null) statueAnimator.SetTrigger("Awaken");
        }
    }
}