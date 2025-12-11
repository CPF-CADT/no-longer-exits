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
    public float deathDelayTime = 2.0f; // Time to wait before respawning

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

        // Correct Model Rotation
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
        CalculatePlayerSpeed();

        // 1. Check if we should chase the player
        if (player != null && !alreadyTriggeredScare)
        {
            if (HideBox.IsPlayerHiddenAnywhere())
            {
                followingPlayer = false;
            }
            else
            {
                bool canSee = CheckVision();
                bool canHear = CheckHearing();

                if (canSee || canHear)
                {
                    followingPlayer = true;
                }
                else if (followingPlayer)
                {
                    float dist = Vector3.Distance(transform.position, player.position);
                    if (dist > visionRange && dist > hearingRange)
                    {
                        followingPlayer = false;
                    }
                }
            }
        }

        // 2. Movement Logic
        if (!alreadyTriggeredScare)
        {
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
        }

        // 3. Other Behaviors
        CheckNearbyDoors();
        CheckCloseDetection();
        HoverMotion();
        RotateBody();
        RotateModel();
    }

    private void CalculatePlayerSpeed()
    {
        if (player == null) return;
        
        // Prevent massive speed spikes if player teleported (respawned) far away
        float moveDist = (player.position - lastPlayerPosition).magnitude;
        if (moveDist > 5f) 
        {
            playerCurrentSpeed = 0f;
        }
        else 
        {
            playerCurrentSpeed = moveDist / Time.deltaTime;
        }

        lastPlayerPosition = player.position;
    }

    private void CheckNearbyDoors()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, doorInteractRange);
        foreach (var hit in hitColliders)
        {
            DoorController door = hit.GetComponentInParent<DoorController>();
            if (door != null)
            {
                door.OpenDoor();
            }
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
        if (distToPlayer <= hearingRange && playerCurrentSpeed > runSpeedThreshold)
        {
            return true;
        }
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
        if (alreadyTriggeredScare) return;

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

    // --- DEATH & RESPAWN LOGIC ---

    private void CheckCloseDetection()
    {
        if (player == null || alreadyTriggeredScare) return;
        if (HideBox.IsPlayerHiddenAnywhere()) return;

        Transform targetPart = model != null ? model : transform;
        float dist = Vector3.Distance(targetPart.position, player.position);

        if (dist <= scareDistance)
        {
            alreadyTriggeredScare = true;

            // 1. FREEZE GHOST
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            transform.LookAt(player);
            if (model != null) model.LookAt(player.position + Vector3.up * 1.5f);

            // 2. DISABLE PLAYER CONTROLS
            TogglePlayerControls(false);

            // 3. TRIGGER CAMERA SCARES
            if (GhostCameraController.Instance != null)
            {
                GhostCameraController.Instance.MoveCameraToPoint(targetPart, 0.5f);
            }

            // 4. START DEATH SEQUENCE
            StartCoroutine(HandleDeathSequence());
        }
    }

    private IEnumerator HandleDeathSequence()
    {
        // 1. Wait for the scare animation (Ghost screaming face)
        yield return new WaitForSeconds(deathDelayTime);

        // 2. FORCE CAMERA RESET (Crucial Fix)
        // This ensures the camera detaches from the ghost and snaps back to the player
        if (GhostCameraController.Instance != null)
        {
            GhostCameraController.Instance.ResetCamera();
        }

        // 3. Call SaveManager to Respawn/Teleport Player
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RespawnPlayer();
        }
        else
        {
            // Fallback: Reload scene if no SaveManager exists
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // 4. Wait a split second for physics to settle after teleport
        yield return new WaitForSeconds(0.1f);

        // 5. Reset Ghost State so it stops chasing/scaring
        ResetGhostState();

        // 6. Give Control back to Player
        TogglePlayerControls(true);
    }

    private void ResetGhostState()
    {
        alreadyTriggeredScare = false;
        followingPlayer = false; // Stop chasing
        agent.isStopped = false; // Resume walking

        // Reset player position tracker so ghost doesn't "hear" the teleport
        if (player != null) lastPlayerPosition = player.position;

        // Force ghost to pick a new random waypoint
        GoToNextWaypoint();
    }

    private void TogglePlayerControls(bool state)
    {
        if (player == null) return;

        FirstPersonLook lookScript = player.GetComponentInChildren<FirstPersonLook>();
        if (lookScript == null && Camera.main != null)
            lookScript = Camera.main.GetComponent<FirstPersonLook>();

        if (lookScript != null)
        {
            lookScript.freezeCamera = !state;
            lookScript.enabled = state;
        }

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

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * visionRange);
    }
}