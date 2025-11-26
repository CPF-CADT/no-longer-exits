using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("Player Detection")]
    public float detectionDistance = 15f; // Detect player if closer than 15 units
    public float maxFollowDistance = 20f; // Stop following if player goes beyond this

    [Header("Flying Settings")]
public float hoverHeight = 0.2f;     // Base hover above ground
public float hoverAmplitude = 0.8f;  // Slight floating (optional)
public float hoverFrequency = 1f;     // Floating speed

    public bool followingPlayer = false;
    private List<Transform> originalWaypoints = new List<Transform>();
    private int currentWaypointIndex = -1;

    private void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (model == null)
            Debug.LogError("Assign your model (child object) to the script!");

        modelForwardOffset = new Vector3(-90f, 0f, 90f);
        model.localRotation = Quaternion.Euler(modelForwardOffset);

        // Save original waypoints
        originalWaypoints.AddRange(waypoints);

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        GoToNextWaypoint();
    }

    private void Update()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            // Follow player only if within detection AND max follow distance
            followingPlayer = distance <= detectionDistance && distance <= maxFollowDistance;
        }

        if (followingPlayer && player != null)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance < waypointThreshold)
                GoToNextWaypoint();
        }

        HoverMotion();
        RotateBody();
        RotateModel();
    }

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
    // Always hover just above ground
    float baseY = agent.nextPosition.y; // ground height from NavMeshAgent
    float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

    // Limit hover so ghost stays 0.1 - 0.2 units above ground
    float minHover = 0.1f;
    float maxHover = 0.2f;
    float finalHover = Mathf.Clamp(hoverHeight + hoverOffset, minHover, maxHover);

    Vector3 pos = transform.position;
    pos.y = baseY + finalHover;
    transform.position = pos;
}


}
