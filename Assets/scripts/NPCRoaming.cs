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
    [Range(0, 360)] public float viewAngle = 120f;
    public LayerMask obstacleMask; 

    [Header("Sensors - Hearing")]
    public float hearingRange = 10f; 
    public float runSpeedThreshold = 4f; 

    [Header("Sensors - Doors")]
    public float doorInteractRange = 5f; // Ghost opens doors within 5 units
    public LayerMask doorLayer; // Optional: Optimize if doors have a specific layer

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
        CheckNearbyDoors(); // <--- NEW FUNCTION HERE
        CheckCloseDetection();
        HoverMotion();
        RotateBody();
        RotateModel();
    }

    // --- NEW: DOOR LOGIC ---
    private void CheckNearbyDoors()
    {
        // Find all colliders within 5 units
        // You can add 'doorLayer' as a second parameter if you want to optimize performance
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, doorInteractRange);

        foreach (var hit in hitColliders)
        {
            // Look for the DoorController on the object or its parent
            DoorController door = hit.GetComponentInParent<DoorController>();
            
            if (door != null)
            {
                // Force the door open (will ignore it if already open)
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

    private void CalculatePlayerSpeed()
    {
        if (player == null) return;
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
        // Draw Door Interaction Range
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