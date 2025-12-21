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
    // REFERENCE TO NEW SCRIPT
    public GhostAudioController audioController;

    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Movement Speeds")]
    public float roamSpeed = 2.5f;
    public float chaseSpeed = 5.5f;
    public float banishedSpeed = 0f;
    public float acceleration = 8f;
    public float angularSpeed = 720f;

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

    // --- BANISHMENT SETTINGS ---
    [Header("Banishment")]
    public ItemData itemRequiredToBanish;
    public float freezeDuration = 2f;
    private bool isBanished = false;

    [Header("Flying Settings")]
    public float hoverHeight = 0.2f;
    public float hoverAmplitude = 0.8f;
    public float hoverFrequency = 1f;


    [Header("Player Settings")]
    public Transform defaultPlayerSpawn; // Assign in Inspector: default spawn if no save exists
    
    // State Variables
    public bool followingPlayer = false;
    private List<Transform> originalWaypoints = new List<Transform>();
    private int currentWaypointIndex = -1;
    private Vector3 lastPlayerPosition;
    private float playerCurrentSpeed;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Auto-find audio controller if not assigned
        if (audioController == null) audioController = GetComponent<GhostAudioController>();

        // ======== SPEED INIT ========
        agent.speed = roamSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
        agent.stoppingDistance = 0f;

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
        if (isBanished || alreadyTriggeredScare) return;

        CalculatePlayerSpeed();

        // ======== SPEED ENTRY POINT ========
        UpdateSpeed();

        // 1. Detection Logic
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

        // 2. Update Audio State via Controller
        if (audioController != null)
            audioController.UpdateState(followingPlayer);

        // 3. Movement Logic
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

    // ================= SPEED FUNCTION =================
    private void UpdateSpeed()
    {
        if (isBanished || alreadyTriggeredScare)
        {
            agent.speed = banishedSpeed;
            return;
        }

        agent.speed = followingPlayer ? chaseSpeed : roamSpeed;
    }

    public bool AttemptBanish(ItemData itemUsed)
    {
        if (isBanished || alreadyTriggeredScare) return false;

        if (itemRequiredToBanish != null)
        {
            if (itemUsed == null || itemUsed != itemRequiredToBanish)
            {
                Debug.Log("Ghost: That item does not scare me!");
                return false;
            }
        }

        StartCoroutine(BanishRoutine());
        return true;
    }

    private IEnumerator BanishRoutine()
    {
        isBanished = true;
        followingPlayer = false;

        // Trigger Audio Banish
        if (audioController != null) audioController.PlayBanish();

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        yield return new WaitForSeconds(freezeDuration);

        // Hide Ghost
        if (model != null) model.gameObject.SetActive(false);
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Silence Audio while hidden
        if (audioController != null) audioController.StopAudio();

        yield return new WaitForSeconds(10f);

        GoToNextWaypoint();
        agent.Warp(originalWaypoints[currentWaypointIndex].position);

        // Show Ghost
        if (model != null) model.gameObject.SetActive(true);
        if (col != null) col.enabled = true;

        agent.isStopped = false;
        isBanished = false;

        // Restart Audio
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
            agent.speed = 0f;

            // Trigger Audio Scare
            if (audioController != null) audioController.PlayScare();

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

        if (SaveManager.Instance != null) SaveManager.Instance.RespawnPlayer();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        yield return new WaitForSeconds(0.1f);
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
        agent.speed = roamSpeed;

        // Reset Audio
        if (audioController != null) audioController.ResetAudio();

        if (player != null) lastPlayerPosition = player.position;
        GoToNextWaypoint();
    }

    private void MoveGhostToDifferentWaypoint(float minDistanceFromPlayer)
    {
        if (originalWaypoints == null || originalWaypoints.Count == 0) return;
        if (player == null) return;
        int attempts = 0; int chosen = -1;
        while (attempts < 10)
        {
            int idx = Random.Range(0, originalWaypoints.Count);
            if (idx == currentWaypointIndex) { attempts++; continue; }
            if (Vector3.Distance(originalWaypoints[idx].position, player.position) >= minDistanceFromPlayer) { chosen = idx; break; }
            attempts++;
        }
        if (chosen == -1) chosen = Random.Range(0, originalWaypoints.Count);

        if (chosen >= 0 && chosen < originalWaypoints.Count)
        {
            currentWaypointIndex = chosen;
            Vector3 spawnPos = originalWaypoints[chosen].position;
            agent.Warp(spawnPos);
            transform.position = spawnPos;
            if (model != null) model.gameObject.SetActive(true);
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
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
}
