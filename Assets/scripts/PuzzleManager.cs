using UnityEngine;
using System.Collections.Generic; 

public class PuzzleManager : MonoBehaviour, ISaveable
{
    [Header("Settings")]
    public int totalWeapons = 4;
    [SerializeField] private int placedCorrectWeapons = 0;

    // --- TEST MODE ---
    public bool debugAutoSolve = false;
    // -----------------

    [Header("References")]
    public DoorController doorController;
    public Animator statueAnimator;
    public CinematicDirector cinematicDirector;

    [Header("Ghosts to Destroy")]
    // The camera will visit them in this order
    public List<NPCRoaming> ghostsToDestroy;

    [Header("Persistence")]
    public PersistentID persistentID;

    private void Awake()
    {
        if (persistentID == null)
            persistentID = GetComponent<PersistentID>();

        if (persistentID == null)
            Debug.LogError($"[PuzzleManager] ERROR: '{name}' has no PersistentID! Save/Load will fail.");
    }

    private void Start()
    {
        if (debugAutoSolve)
        {
            Debug.Log("[PuzzleManager] TEST MODE: Auto-solving puzzle...");
            placedCorrectWeapons = totalWeapons;
            PuzzleCompleted();
        }
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

        // --- STEP 1: FREEZE ALL GHOSTS IMMEDIATELY ---
        // This stops them from walking away while the camera is flying to them.
        if (ghostsToDestroy != null)
        {
            foreach (var ghost in ghostsToDestroy)
            {
                if (ghost != null) 
                {
                    // Call the StopEverything function in your NPCRoaming script
                    ghost.StopEverything(); 
                }
            }
        }

        // --- STEP 2: OPEN DOOR / ANIMATE STATUE ---
        if (doorController != null)
        {
            doorController.lockedByPuzzle = false;
            doorController.OpenDoor();
        }
        if (statueAnimator != null) statueAnimator.SetTrigger("Awaken");

        // --- STEP 3: START CINEMATIC CAMERA ---
        if (cinematicDirector != null)
        {
            if (ghostsToDestroy != null && ghostsToDestroy.Count > 0)
            {
                // Triggers the camera to fly to the (now frozen) ghosts
                cinematicDirector.StartEndingSequence(ghostsToDestroy);
            }
            else
            {
                Debug.LogWarning("[PuzzleManager] Puzzle Solved, but the 'Ghosts To Destroy' list is empty!");
            }
        }
        else
        {
            Debug.LogWarning("Cinematic Director not assigned in Inspector!");
        }
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

        if (placedCorrectWeapons >= totalWeapons)
        {
            if (doorController != null) { doorController.lockedByPuzzle = false; doorController.OpenDoor(); }
            if (statueAnimator != null) statueAnimator.SetTrigger("Awaken");
            
            // Optional: Hide ghosts if loading a completed game
            if (ghostsToDestroy != null)
            {
                foreach(var ghost in ghostsToDestroy)
                {
                    if(ghost != null) ghost.gameObject.SetActive(false);
                }
            }
        }
    }
}