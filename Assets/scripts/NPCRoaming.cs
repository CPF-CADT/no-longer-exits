using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class NPCRoaming : MonoBehaviour
{
    // --- ADDED FOR DIRECTOR (REQUIRED) ---
    [Header("Cinematic Settings")]
    public Transform cinematicCameraPoint; 
    // -------------------------------------

    // --- NEW: PUZZLE SETTINGS ---
    [Header("Puzzle Control")]
    public bool isPuzzleActive = false; // Check this to freeze ghost
    // ----------------------------

    [Header("References")]
    public NavMeshAgent agent;
    public Transform model;
    public Transform player;
    public GhostAudioController audioController; 

    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Settings")]
    public float waypointThreshold = 1f;
    public float turnSpeed = 5f;
    public Vector3 modelForwardOffset = Vector3.zero;

    [Header("Sensors - Vision")]
    public float visionRange = 15f;
    [Range(0, 360)] public float viewAngle = 120f;
    public LayerMask obstacleMask;

    [Header("Sensors - Hearing")]
    public float hearingRange = 10f;
    public float runSpeedThreshold = 4f;

    [Header("Sensors - Doors")]
    public float doorInteractRange = 5f;

    [Header("Close Range Scare Detection")]
    public float scareDistance = 2f;
    public bool alreadyTriggeredScare = false;
    public float deathDelayTime = 2.0f;
    public float minWaypointDistanceFromPlayer = 6f;

    [Header("Banishment")]
    public ItemData itemRequiredToBanish;
    public float freezeDuration = 2f;
    private bool isBanished = false;

    [Header("Flying Settings")]
    public float hoverHeight = 0.2f;
    public float hoverAmplitude = 0.8f;
    public float hoverFrequency = 1f;

    // State Variables
    public bool followingPlayer = false;
    private List<Transform> originalWaypoints = new List<Transform>();
    private int currentWaypointIndex = -1;
    private Vector3 lastPlayerPosition;
    private float playerCurrentSpeed;

    // --- ADDED FOR DIRECTOR ---
    private bool isFrozenByDirector = false;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (audioController == null) audioController = GetComponent<GhostAudioController>();

        // Auto-create point if missing
        if (cinematicCameraPoint == null)
        {
            GameObject autoPoint = new GameObject("AutoCameraPoint");
            autoPoint.transform.SetParent(this.transform);
            autoPoint.transform.localPosition = new Vector3(0, 1.5f, 2.5f); 
            autoPoint.transform.localRotation = Quaternion.Euler(0, 180, 0); 
            cinematicCameraPoint = autoPoint.transform;
        }

        modelForwardOffset = new Vector3(-90f, 0f, 90f);
        if (model != null) model.localRotation = Quaternion.Euler(modelForwardOffset);

        originalWaypoints.AddRange(waypoints);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                lastPlayerPosition = player.position;
            }
        }

        GoToNextWaypoint();
    }

    void Update()
    {
        // --- 1. STOP IF DIRECTOR OR PUZZLE IS ACTIVE ---
        if (isFrozenByDirector) return; 
        
        if (isPuzzleActive) 
        {
            // Force stop immediately if puzzle is on
            if(agent != null && agent.isActiveAndEnabled && !agent.isStopped) 
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                if (audioController != null) audioController.StopAudio();
            }
            return; 
        }
        // -----------------------------------------------

        if (isBanished || alreadyTriggeredScare) return;

        // Ensure agent is moving if we are back to normal
        if (agent != null && agent.isActiveAndEnabled && agent.isStopped) 
            agent.isStopped = false;

        CalculatePlayerSpeed();

        // 2. Detection
        if (player != null)
        {
            if (HideBox.IsPlayerHiddenAnywhere())
            {
                followingPlayer = false;
            }
            else
            {
                bool canSee = CheckVision();
                bool canHear = CheckHearing();

                if (canSee || canHear) followingPlayer = true;
                else if (followingPlayer)
                {
                    float dist = Vector3.Distance(transform.position, player.position);
                    if (dist > visionRange && dist > hearingRange) followingPlayer = false;
                }
            }
        }

        // 3. Audio
        if (audioController != null) audioController.UpdateState(followingPlayer);

        // 4. Movement
        if (followingPlayer && player != null)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance < waypointThreshold)
            {
                GoToNextWaypoint();
            }
        }

        CheckNearbyDoors();
        CheckCloseDetection();
        HoverMotion();
        RotateBody();
        RotateModel();
    }

    // --- NEW FUNCTION: CALL THIS FROM YOUR PUZZLE SCRIPT ---
    public void SetPuzzleMode(bool isActive)
    {
        isPuzzleActive = isActive;
        
        if (isActive)
        {
            // STOP immediately
            if (agent != null) 
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            if (audioController != null) audioController.StopAudio();
        }
        else
        {
            // RESUME
            if (agent != null) agent.isStopped = false;
            if (audioController != null) audioController.ResetAudio();
        }
    }
    // ------------------------------------------------------

    // --- ADDED FUNCTION FOR DIRECTOR ---
    public void StopEverything()
    {
        isFrozenByDirector = true;
        followingPlayer = false;
        if (agent != null) 
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        if (audioController != null) audioController.StopAudio();
    }
    // ----------------------------------

    public bool AttemptBanish(ItemData itemUsed)
    {
        if (isBanished || alreadyTriggeredScare || isFrozenByDirector || isPuzzleActive) return false;

        if (itemRequiredToBanish != null)
        {
            if (itemUsed == null || itemUsed != itemRequiredToBanish) return false;
        }

        StartCoroutine(BanishRoutine());
        return true;
    }

    private IEnumerator BanishRoutine()
    {
        isBanished = true;
        followingPlayer = false;
        if (audioController != null) audioController.PlayBanish();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        yield return new WaitForSeconds(freezeDuration);

        if (model != null) model.gameObject.SetActive(false);
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (audioController != null) audioController.StopAudio();

        yield return new WaitForSeconds(10f);

        GoToNextWaypoint();
        agent.Warp(originalWaypoints[currentWaypointIndex].position);

        if (model != null) model.gameObject.SetActive(true);
        if (col != null) col.enabled = true;
        agent.isStopped = false;
        isBanished = false;
        if (audioController != null) audioController.ResetAudio();
    }

    private void CalculatePlayerSpeed()
    {
        if (player == null) return;
        float moveDist = (player.position - lastPlayerPosition).magnitude;
        if (moveDist > 5f) playerCurrentSpeed = 0f;
        else playerCurrentSpeed = moveDist / Time.deltaTime;
        lastPlayerPosition = player.position;
    }

    private void CheckNearbyDoors()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, doorInteractRange);
        foreach (var hit in hitColliders)
        {
            DoorController door = hit.GetComponentInParent<DoorController>();
            if (door != null) door.OpenDoor();
        }
    }

    private bool CheckVision()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer > visionRange) return false;
        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distToPlayer, obstacleMask)) return true;
        }
        return false;
    }

    private bool CheckHearing()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= hearingRange && playerCurrentSpeed > runSpeedThreshold) return true;
        return false;
    }

    private void GoToNextWaypoint()
    {
        if (originalWaypoints.Count == 0) return;
        currentWaypointIndex = Random.Range(0, originalWaypoints.Count);
        agent.SetDestination(originalWaypoints[currentWaypointIndex].position);
    }

    private void RotateBody()
    {
        if (alreadyTriggeredScare || isBanished) return;
        Vector3 velocity = agent.velocity;
        velocity.y = 0;
        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    private void RotateModel()
    {
        if (model != null) model.localRotation = Quaternion.Euler(modelForwardOffset);
    }

    // YOUR ORIGINAL HOVER FUNCTION (UNTOUCHED)
    private void HoverMotion()
    {
        float baseY = agent.nextPosition.y;
        float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Clamp(hoverHeight + hoverOffset, 0.1f, 0.2f);
        transform.position = pos;
    }

    private void CheckCloseDetection()
    {
        if (player == null || alreadyTriggeredScare || isBanished) return;
        if (HideBox.IsPlayerHiddenAnywhere()) return;

        Transform targetPart = model != null ? model : transform;
        float dist = Vector3.Distance(targetPart.position, player.position);

        if (dist <= scareDistance)
        {
            alreadyTriggeredScare = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            if (audioController != null) audioController.PlayScare();

            transform.LookAt(player);
            if (model != null) model.LookAt(player.position + Vector3.up * 1.5f);
            
            // NOTE: Director handles camera now, but leaving your hooks here just in case
            if (GhostCameraController.Instance != null)
                GhostCameraController.Instance.MoveCameraToPoint(targetPart, 0.5f);

            StartCoroutine(HandleDeathSequence());
        }
    }

    private IEnumerator HandleDeathSequence()
    {
        yield return new WaitForSeconds(deathDelayTime);
        if (GhostCameraController.Instance != null) GhostCameraController.Instance.ResetCamera();

        if (SaveManager.Instance != null) SaveManager.Instance.RespawnPlayer();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        yield return new WaitForSeconds(0.1f);
        
        // Use your original way to find a new spot
        if (originalWaypoints.Count > 0)
        {
             currentWaypointIndex = Random.Range(0, originalWaypoints.Count);
             agent.Warp(originalWaypoints[currentWaypointIndex].position);
        }

        ResetGhostState();
    }

    private void ResetGhostState()
    {
        alreadyTriggeredScare = false;
        followingPlayer = false;
        agent.isStopped = false;
        isBanished = false;
        isFrozenByDirector = false; // Reset director flag too
        isPuzzleActive = false; // Reset puzzle flag if player died
        
        if (audioController != null) audioController.ResetAudio();
        if (player != null) lastPlayerPosition = player.position;
        GoToNextWaypoint();
    }
}