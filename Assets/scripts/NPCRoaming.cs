using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class NPCRoaming : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform model;
    public Transform player;

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
    [Tooltip("Minimum distance from player for chosen respawn waypoint after a scare")]
    public float minWaypointDistanceFromPlayer = 6f;

    // --- BANISHMENT SETTINGS ---
    [Header("Banishment (Item Protection)")]
    public ItemData itemRequiredToBanish; // <--- Drag your 'YounItem' Data here
    public float freezeDuration = 2f;          
    private bool isBanished = false;
    // ---------------------------

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

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

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
        // 0. If Banished or Scaring, do nothing
        if (isBanished || alreadyTriggeredScare) return;

        CalculatePlayerSpeed();

        // 1. Check if we should chase the player
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

        // 2. Movement Logic
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

        // 3. Other Behaviors
        CheckNearbyDoors();
        CheckCloseDetection();
        HoverMotion();
        RotateBody();
        RotateModel();
    }

    // --- NEW FUNCTION TO FIX THE ERROR ---
    // This allows PlayerInteract to send the ItemData
    public bool AttemptBanish(ItemData itemUsed)
    {
        if (isBanished || alreadyTriggeredScare) return false;

        // Check if the item matches
        if (itemRequiredToBanish != null)
        {
            if (itemUsed == null || itemUsed != itemRequiredToBanish)
            {
                Debug.Log("Ghost: That item does not scare me!");
                return false; // Wrong item, do nothing
            }
        }

        // If correct, start the routine
        StartCoroutine(BanishRoutine());
        return true;
    }
    // -------------------------------------

    private IEnumerator BanishRoutine()
    {
        isBanished = true;
        followingPlayer = false;

        // 1. Stop Moving immediately
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 2. Wait for 2 seconds frozen (Hit effect)
        yield return new WaitForSeconds(freezeDuration);

        // 3. Disable (Hide Ghost)
        if (model != null) model.gameObject.SetActive(false);
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4. Wait EXACTLY 10 seconds (Your Logic)
        yield return new WaitForSeconds(10f);

        // 5. Respawn Randomly
        GoToNextWaypoint(); 
        agent.Warp(originalWaypoints[currentWaypointIndex].position); 

        // 6. Enable (Show Ghost)
        if (model != null) model.gameObject.SetActive(true);
        if (col != null) col.enabled = true;

        // 7. Reset State
        agent.isStopped = false;
        isBanished = false;
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

    // --- SENSOR LOGIC ---
    private bool CheckVision()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer > visionRange) return false;

        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distToPlayer, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckHearing()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= hearingRange && playerCurrentSpeed > runSpeedThreshold) return true;
        return false;
    }

    // --- MOVEMENT & VISUALS ---
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

    private void HoverMotion()
    {
        float baseY = agent.nextPosition.y;
        float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Clamp(hoverHeight + hoverOffset, 0.1f, 0.2f);
        transform.position = pos;
    }

    // --- DEATH LOGIC ---
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
            transform.LookAt(player);
            if (model != null) model.LookAt(player.position + Vector3.up * 1.5f);
            TogglePlayerControls(false);

            if (GhostCameraController.Instance != null)
                GhostCameraController.Instance.MoveCameraToPoint(targetPart, 0.5f);

            StartCoroutine(HandleDeathSequence());
        }
    }

    private IEnumerator HandleDeathSequence()
    {
        yield return new WaitForSeconds(deathDelayTime);
        if (GhostCameraController.Instance != null) GhostCameraController.Instance.ResetCamera();

        // Respawn the player first (teleport player to saved location)
        if (SaveManager.Instance != null) SaveManager.Instance.RespawnPlayer();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        yield return new WaitForSeconds(0.1f);
        // Move ghost away from the player's new location to avoid immediate re-triggering
        MoveGhostToDifferentWaypoint(minWaypointDistanceFromPlayer);
        ResetGhostState();
        TogglePlayerControls(true);
    }

    private void ResetGhostState()
    {
        alreadyTriggeredScare = false;
        followingPlayer = false;
        agent.isStopped = false;
        isBanished = false; 
        if (player != null) lastPlayerPosition = player.position;
        GoToNextWaypoint();
    }

    private void MoveGhostToDifferentWaypoint(float minDistanceFromPlayer)
    {
        if (originalWaypoints == null || originalWaypoints.Count == 0) return;
        if (player == null) return;

        int attempts = 0;
        int chosen = -1;
        // Try to find a waypoint that is not the current one and is sufficiently far from the player
        while (attempts < 10)
        {
            int idx = Random.Range(0, originalWaypoints.Count);
            if (idx == currentWaypointIndex) { attempts++; continue; }
            Transform wp = originalWaypoints[idx];
            if (wp == null) { attempts++; continue; }
            float distToPlayer = Vector3.Distance(wp.position, player.position);
            if (distToPlayer >= minDistanceFromPlayer)
            {
                chosen = idx;
                break;
            }
            attempts++;
        }

        // If none found, pick any different waypoint
        if (chosen == -1)
        {
            if (originalWaypoints.Count == 1) chosen = 0;
            else
            {
                chosen = Random.Range(0, originalWaypoints.Count);
                if (chosen == currentWaypointIndex) chosen = (chosen + 1) % originalWaypoints.Count;
            }
        }

        if (chosen >= 0 && chosen < originalWaypoints.Count && originalWaypoints[chosen] != null)
        {
            currentWaypointIndex = chosen;
            Vector3 spawnPos = originalWaypoints[chosen].position;
            agent.Warp(spawnPos);
            transform.position = spawnPos;
            // ensure model and collider are active
            if (model != null) model.gameObject.SetActive(true);
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            // pick a new destination to continue roaming
            GoToNextWaypoint();
        }
    }

    private void TogglePlayerControls(bool state)
    {
        if (player == null) return;
        FirstPersonLook lookScript = player.GetComponentInChildren<FirstPersonLook>();
        if (lookScript == null && Camera.main != null) lookScript = Camera.main.GetComponent<FirstPersonLook>();
        if (lookScript != null) { lookScript.freezeCamera = !state; lookScript.enabled = state; }

        FirstPersonMovement moveScript = player.GetComponent<FirstPersonMovement>();
        if (moveScript != null) moveScript.enabled = state;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = !state;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, doorInteractRange);
        if (player == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}