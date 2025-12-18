using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour, ISaveable
{
    [Header("Settings")]
    public int totalWeapons = 4;
    [SerializeField] private int placedCorrectWeapons = 0;

    [Header("Debug")]
    public bool debugAutoSolve = false;

    [Header("References")]
    public Animator statueAnimator;
    public CinematicDirector cinematicDirector;

    // ================= PORTAL SETTINGS =================
    [Header("Portal")]
    [Tooltip("Portal Door (parent object with collider + PortalSceneLoader)")]
    public GameObject portalDoor;

    [Tooltip("Portal Visual prefab (VFX child)")]
    public GameObject portalVisualPrefab;

    [Tooltip("Use custom spawn position & rotation for the visual only")]
    public bool useCustomPortalTransform = true;

    [Tooltip("Local position of the portal visual relative to portalDoor")]
    public Vector3 portalPosition;

    [Tooltip("Local rotation of the portal visual relative to portalDoor")]
    public Vector3 portalRotationEuler;

    private GameObject spawnedPortalVisual;

    // ================= GHOSTS =================
    [Header("Ghosts to Destroy")]
    public List<NPCRoaming> ghostsToDestroy;

    // ================= SAVE =================
    [Header("Persistence")]
    public PersistentID persistentID;

    private bool puzzleSolved = false;

    private void Awake()
    {
        if (persistentID == null)
            persistentID = GetComponent<PersistentID>();

        if (persistentID == null)
            Debug.LogError($"[PuzzleManager] '{name}' has no PersistentID!");

        // Ensure portal is OFF at start
        if (portalDoor != null)
            portalDoor.SetActive(false);
    }

    private void Start()
    {
        if (debugAutoSolve)
        {
            placedCorrectWeapons = totalWeapons;
            PuzzleCompleted();
        }
    }

    // ================= WEAPON EVENTS =================

    public void NotifyWeaponPlaced()
    {
        if (puzzleSolved) return;

        placedCorrectWeapons++;
        placedCorrectWeapons = Mathf.Clamp(placedCorrectWeapons, 0, totalWeapons);

        Debug.Log($"[PuzzleManager] Progress: {placedCorrectWeapons}/{totalWeapons}");

        if (placedCorrectWeapons >= totalWeapons)
            PuzzleCompleted();
    }

    public void NotifyWeaponRemoved()
    {
        if (puzzleSolved) return;

        placedCorrectWeapons--;
        placedCorrectWeapons = Mathf.Max(0, placedCorrectWeapons);
    }

    // ================= PUZZLE COMPLETE =================

    private void PuzzleCompleted()
    {
        if (puzzleSolved) return;
        puzzleSolved = true;

        Debug.Log("[PuzzleManager] PUZZLE SOLVED!");

        // 1. Stop ghosts
        if (ghostsToDestroy != null)
        {
            foreach (var ghost in ghostsToDestroy)
            {
                if (ghost != null)
                    ghost.StopEverything();
            }
        }

        // 2. Enable portal
        EnablePortal();

        // 3. Statue animation
        if (statueAnimator != null)
            statueAnimator.SetTrigger("Awaken");

        // 4. Cinematic
        if (cinematicDirector != null && ghostsToDestroy != null && ghostsToDestroy.Count > 0)
        {
            cinematicDirector.StartEndingSequence(ghostsToDestroy);
        }
    }

    // ================= PORTAL LOGIC =================

    private void EnablePortal()
    {
        if (portalDoor == null)
        {
            Debug.LogWarning("[PuzzleManager] PortalDoor not assigned!");
            return;
        }

        portalDoor.SetActive(true);

        SpawnPortalVisual();

        Debug.Log("[PuzzleManager] Portal enabled");
    }

    private void SpawnPortalVisual()
    {
        if (portalVisualPrefab == null)
        {
            Debug.LogWarning("[PuzzleManager] PortalVisualPrefab not assigned!");
            return;
        }

        // Remove old visual if exists
        if (spawnedPortalVisual != null)
            Destroy(spawnedPortalVisual);

        // Spawn visual as CHILD of portalDoor
        spawnedPortalVisual = Instantiate(
            portalVisualPrefab,
            portalDoor.transform.position,
            portalDoor.transform.rotation,
            portalDoor.transform
        );

        // Apply custom local offset if enabled
        if (useCustomPortalTransform)
        {
            spawnedPortalVisual.transform.localPosition = portalPosition;
            spawnedPortalVisual.transform.localRotation = Quaternion.Euler(portalRotationEuler);
        }

        RestartPortalVFX(spawnedPortalVisual);
    }

    private void RestartPortalVFX(GameObject portalVisual)
    {
        ParticleSystem[] systems = portalVisual.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in systems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);
        }
    }

    // ================= SAVE / LOAD =================

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
        if (state == null || state.type != "Puzzle")
            return;

        placedCorrectWeapons = state.puzzlePlacedCorrect;

        if (placedCorrectWeapons >= totalWeapons)
        {
            puzzleSolved = true;
            EnablePortal();

            if (statueAnimator != null)
                statueAnimator.SetTrigger("Awaken");

            if (ghostsToDestroy != null)
            {
                foreach (var ghost in ghostsToDestroy)
                {
                    if (ghost != null)
                        ghost.gameObject.SetActive(false);
                }
            }
        }
    }
}
