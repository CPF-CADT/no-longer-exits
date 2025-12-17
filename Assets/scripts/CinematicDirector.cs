using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class CinematicDirector : MonoBehaviour
{
    [Header("Camera Setup")]
    public Camera cinematicCamera;
    public Transform playerCamera;

    [Header("Chase Settings")]
    public float movementSpeed = 6f;
    public float rotationSpeed = 5f;

    [Header("Detection Settings")]
    [Tooltip("Distance to start locking onto the ghost.")]
    public float detectDistance = 5.0f;
    public LayerMask obstructionMask;

    [Header("Ghostly Movement")]
    public float floatBaseHeight = 1.4f;
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 1.0f;

    [Header("Look At Adjustment")]
    public float lookAtHeightOffset = 1.5f;

    [Header("Timing")]
    public float delayBeforeDestruction = 1.5f;
    public float timeBetweenGhosts = 0.5f;

    [Header("Destruction")]
    public GameObject explosionVFX; // Triggers when camera DETECTS ghost now

    private bool isCinematicActive = false;
    private NavMeshAgent cameraAgent;

    private void Awake()
    {
        if (cinematicCamera != null)
        {
            cameraAgent = cinematicCamera.GetComponent<NavMeshAgent>();
            if (cameraAgent != null)
            {
                cameraAgent.updatePosition = false;
                cameraAgent.updateRotation = false;
                cameraAgent.updateUpAxis = true;
            }
            cinematicCamera.gameObject.SetActive(false);
        }
    }

    public void StartEndingSequence(List<NPCRoaming> allGhosts)
    {
        if (isCinematicActive) return;
        isCinematicActive = true;
        StartCoroutine(DynamicMultiDeathSequence(allGhosts));
    }

    private IEnumerator DynamicMultiDeathSequence(List<NPCRoaming> ghosts)
    {
        Debug.Log($"[Director] Sequence Started.");

        TogglePlayerControls(false);

        if (cinematicCamera != null && playerCamera != null)
        {
            cinematicCamera.transform.position = playerCamera.position;
            cinematicCamera.transform.rotation = playerCamera.rotation;
            cinematicCamera.enabled = true;
            cinematicCamera.gameObject.SetActive(true);

            if (cameraAgent != null)
            {
                cameraAgent.enabled = true;
                cameraAgent.Warp(playerCamera.position);
                cameraAgent.speed = movementSpeed;
                cameraAgent.isStopped = false;
            }
        }

        foreach (NPCRoaming currentGhost in ghosts)
        {
            if (currentGhost == null) continue;

            // --- PHASE 1: THE HUNT ---
            bool lockedOn = false;
            cinematicCamera.transform.SetParent(null);

            if (cameraAgent != null)
            {
                cameraAgent.enabled = true;
                cameraAgent.isStopped = false;
            }

            while (!lockedOn)
            {
                if (currentGhost == null) break;

                // 1. Target
                Vector3 targetPos = currentGhost.transform.position;
                if (currentGhost.cinematicCameraPoint != null)
                    targetPos = currentGhost.cinematicCameraPoint.position;

                if (cameraAgent != null) cameraAgent.SetDestination(targetPos);

                // 2. Move
                if (cameraAgent != null)
                {
                    Vector3 floorPos = cameraAgent.nextPosition;
                    float hoverOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
                    Vector3 finalPos = floorPos;
                    finalPos.y += floatBaseHeight + hoverOffset;
                    cinematicCamera.transform.position = finalPos;
                }

                // 3. Rotation
                Vector3 ghostHead = currentGhost.transform.position + Vector3.up * lookAtHeightOffset;
                float distToGhost = Vector3.Distance(cinematicCamera.transform.position, targetPos);
                Quaternion desiredRotation;

                if (distToGhost > 8.0f && cameraAgent.velocity.sqrMagnitude > 0.1f)
                    desiredRotation = Quaternion.LookRotation(cameraAgent.velocity.normalized);
                else
                {
                    Vector3 dirToGhost = (ghostHead - cinematicCamera.transform.position).normalized;
                    desiredRotation = Quaternion.LookRotation(dirToGhost);
                }

                cinematicCamera.transform.rotation = Quaternion.Slerp(
                    cinematicCamera.transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime
                );

                // 4. Check Lock
                if (distToGhost <= detectDistance)
                {
                    RaycastHit hit;
                    Vector3 dirCheck = (ghostHead - cinematicCamera.transform.position).normalized;
                    if (!Physics.Raycast(cinematicCamera.transform.position, dirCheck, out hit, distToGhost, obstructionMask))
                    {
                        lockedOn = true;

                        // --- PLAY VFX HERE (UPON DETECTION) ---
                        if (explosionVFX != null)
                        {
                            GameObject vfx = Instantiate(
                                explosionVFX,
                                currentGhost.transform.position + Vector3.up,
                                Quaternion.identity
                            );

                            vfx.transform.localScale = Vector3.one * 0.25f;
                        }

                    }
                }
                yield return null;
            }

            // --- PHASE 2: STOP & ATTACH ---
            if (currentGhost != null) currentGhost.StopEverything();

            if (cameraAgent != null) cameraAgent.enabled = false;

            if (currentGhost != null && currentGhost.cinematicCameraPoint != null)
            {
                cinematicCamera.transform.SetParent(currentGhost.cinematicCameraPoint);

                float snapTimer = 0f;
                Vector3 startPos = cinematicCamera.transform.localPosition;
                Quaternion startRot = cinematicCamera.transform.localRotation;

                while (snapTimer < 1.0f)
                {
                    snapTimer += Time.deltaTime * 2.0f; // Fast snap
                    cinematicCamera.transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, snapTimer);
                    cinematicCamera.transform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, snapTimer);
                    yield return null;
                }
            }

            // --- PHASE 3: EXECUTE ---
            // Wait for the duration (VFX played at start of detection, so it's visible now)
            yield return new WaitForSeconds(delayBeforeDestruction);

            // Just hide the ghost (VFX already played)
            if (currentGhost != null)
                currentGhost.gameObject.SetActive(false);

            cinematicCamera.transform.SetParent(null);
            yield return new WaitForSeconds(timeBetweenGhosts);
        }

        Debug.Log("[Director] All ghosts destroyed.");
        yield return new WaitForSeconds(2f);
    }

    private void TogglePlayerControls(bool state)
    {
        if (playerCamera == null) return;
        Camera playerCam = playerCamera.GetComponent<Camera>();
        if (playerCam == null) playerCam = playerCamera.GetComponentInChildren<Camera>();
        if (playerCam != null) playerCam.enabled = state;

        AudioListener list = playerCamera.GetComponent<AudioListener>();
        if (list == null) list = playerCamera.GetComponentInChildren<AudioListener>();
        if (list != null) list.enabled = state;

        Transform root = playerCamera.root;
        MonoBehaviour[] scripts = root.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s == this) continue;
            string n = s.GetType().Name;
            if (n.Contains("Move") || n.Contains("Look") || n.Contains("Controller") || n.Contains("Input"))
                s.enabled = state;
        }
    }
}