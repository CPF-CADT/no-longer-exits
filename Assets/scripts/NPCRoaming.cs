using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCRoaming : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform model;
    public Transform player;
    public Transform cameraScarePoint;

    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Settings")]
    public float waypointThreshold = 1f;
    public float turnSpeed = 5f;
    public Vector3 modelForwardOffset = Vector3.zero;

    [Header("Sensors - Vision")]
    public float visionRange = 15f;
    [Range(0, 360)] public float viewAngle = 90f; // 90 Degree View
    public LayerMask obstacleMask; // Layers that block vision (Walls, etc.)

    [Header("Sensors - Hearing")]
    public float hearingRange = 10f; // Detect running within 8-10 units
    public float runSpeedThreshold = 4f; // Minimum speed to be considered "Running"

    [Header("Close Range Scare Detection")]
    public float scareDistance = 2f;
    public bool alreadyTriggeredScare = false;

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
            if (playerObj != null) player = playerObj.transform;
        }

        GoToNextWaypoint();
    }

    void Update()
    {
        // Calculate Player Speed for Hearing Logic
        CalculatePlayerSpeed();

        // 1. Check if we should chase the player (Vision or Hearing)
        if (player != null)
        {
            // If player is hiding, we lose track immediately
            if (HideBox.IsPlayerHiddenAnywhere())
            {
                followingPlayer = false;
            }
            else
            {
                bool canSee = CheckVision();
                bool canHear = CheckHearing();

                // If we see OR hear the player, we follow
                if (canSee || canHear)
                {
                    followingPlayer = true;
                }
                // Determine when to stop following (if player gets too far away)
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
        if (followingPlayer && player != null)
        {
            // IGNORE waypoints, set Player as the destination
            agent.SetDestination(player.position);
        }
        else
        {
            // Patrol Logic
            if (!agent.pathPending && agent.remainingDistance < waypointThreshold)
            {
                GoToNextWaypoint();
            }
        }

        // 3. Other Behaviors
        CheckCloseDetection();
        HoverMotion();
        RotateBody();
        RotateModel();
    }

    // --- SENSOR LOGIC ---

    private bool CheckVision()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Check Distance
        if (distToPlayer > visionRange) return false;

        // 2. Check Angle (90 degrees)
        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
        {
            // 3. Check Raycast (Line of Sight)
            // We cast from slightly up (0.5f) to avoid hitting the floor immediately
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distToPlayer, obstacleMask))
            {
                // If the ray didn't hit an obstacle, we see the player
                return true;
            }
        }
        return false;
    }

    private bool CheckHearing()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // If player is within hearing range (10 units) AND moving fast
        if (distToPlayer <= hearingRange && playerCurrentSpeed > runSpeedThreshold)
        {
            return true;
        }
        return false;
    }

    private void CalculatePlayerSpeed()
    {
        if (player == null) return;
        // Simple speed calculation based on position change
        playerCurrentSpeed = (player.position - lastPlayerPosition).magnitude / Time.deltaTime;
        lastPlayerPosition = player.position;
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
        if (model != null)
            model.localRotation = Quaternion.Euler(modelForwardOffset);
    }

    private void HoverMotion()
    {
        float baseY = agent.nextPosition.y;
        float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float minHover = 0.1f;
        float maxHover = 0.2f;
        float finalHover = Mathf.Clamp(hoverHeight + hoverOffset, minHover, maxHover);
        Vector3 pos = transform.position;
        pos.y = baseY + finalHover;
        transform.position = pos;
    }

    private void CheckCloseDetection()
    {
        if (player == null || alreadyTriggeredScare) return;
        if (HideBox.IsPlayerHiddenAnywhere()) return;

        // Use the Model position for distance (more accurate for floating ghosts)
        Transform targetPart = model != null ? model : transform;
        float dist = Vector3.Distance(targetPart.position, player.position);

        if (dist <= scareDistance)
        {
            alreadyTriggeredScare = true;

            // 1. FREEZE
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            transform.LookAt(player);
            // Ensure the floating head looks at the player too
            if (model != null) model.LookAt(player.position + Vector3.up * 1.5f); 

            // 2. DISABLE PLAYER
            FirstPersonLook lookScript = player.GetComponentInChildren<FirstPersonLook>();
            if (lookScript == null && Camera.main != null) 
                lookScript = Camera.main.GetComponent<FirstPersonLook>();
            
            if (lookScript != null) {
                lookScript.freezeCamera = true;
                lookScript.enabled = false; 
            }

            FirstPersonMovement moveScript = player.GetComponent<FirstPersonMovement>();
            if (moveScript != null) moveScript.enabled = false;
            
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // 3. TRIGGER CAMERA
            if (GhostCameraController.Instance != null)
            {
                // *** THE FIX: Pass 'targetPart' (The Model) instead of 'transform' ***
                // This ensures the camera goes to the FLOATING HEAD, not the feet on the ground.
                GhostCameraController.Instance.MoveCameraToPoint(targetPart, 0.5f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // --- Draw Vision Range Circle ---
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // --- Draw Vision Angle Lines ---
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * visionRange);

        // --- Draw Line to Player ---
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= visionRange && Vector3.Angle(transform.forward, dirToPlayer) <= viewAngle / 2)
        {
            // Check if line of sight is clear
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPlayer, distToPlayer, obstacleMask))
            {
                Gizmos.color = Color.green; // Player visible
            }
            else
            {
                Gizmos.color = Color.red; // Player blocked
            }
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

}