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

    [Header("Return To Player")]
    public float returnDuration = 1.0f;

    [Header("Destruction")]
    public GameObject explosionVFX;

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
                cameraAgent.enabled = false;
            }

            cinematicCamera.enabled = false;
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
        Debug.Log("[Director] Sequence Started.");

        TogglePlayerControls(false);

        // Sync cinematic camera with player camera
        cinematicCamera.transform.position = playerCamera.position;
        cinematicCamera.transform.rotation = playerCamera.rotation;
        cinematicCamera.gameObject.SetActive(true);
        cinematicCamera.enabled = true;

        if (cameraAgent != null)
        {
            cameraAgent.enabled = true;
            cameraAgent.Warp(playerCamera.position);
            cameraAgent.speed = movementSpeed;
            cameraAgent.isStopped = false;
        }

        foreach (NPCRoaming currentGhost in ghosts)
        {
            if (currentGhost == null) continue;

            bool lockedOn = false;
            cinematicCamera.transform.SetParent(null);

            if (cameraAgent != null)
            {
                cameraAgent.enabled = true;
                cameraAgent.isStopped = false;
            }

            // ---------- PHASE 1: HUNT ----------
            while (!lockedOn)
            {
                if (currentGhost == null) break;

                Vector3 targetPos = currentGhost.transform.position;
                if (currentGhost.cinematicCameraPoint != null)
                    targetPos = currentGhost.cinematicCameraPoint.position;

                if (cameraAgent != null)
                    cameraAgent.SetDestination(targetPos);

                if (cameraAgent != null)
                {
                    Vector3 floorPos = cameraAgent.nextPosition;
                    float hover = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
                    cinematicCamera.transform.position =
                        new Vector3(floorPos.x, floorPos.y + floatBaseHeight + hover, floorPos.z);
                }

                Vector3 ghostHead = currentGhost.transform.position + Vector3.up * lookAtHeightOffset;
                float dist = Vector3.Distance(cinematicCamera.transform.position, targetPos);

                Quaternion lookRot;
                if (dist > 8f && cameraAgent.velocity.sqrMagnitude > 0.1f)
                    lookRot = Quaternion.LookRotation(cameraAgent.velocity.normalized);
                else
                    lookRot = Quaternion.LookRotation((ghostHead - cinematicCamera.transform.position).normalized);

                cinematicCamera.transform.rotation = Quaternion.Slerp(
                    cinematicCamera.transform.rotation,
                    lookRot,
                    rotationSpeed * Time.deltaTime
                );

                if (dist <= detectDistance)
                {
                    if (!Physics.Raycast(cinematicCamera.transform.position,
                        (ghostHead - cinematicCamera.transform.position).normalized,
                        dist,
                        obstructionMask))
                    {
                        lockedOn = true;

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

            // ---------- PHASE 2: ATTACH ----------
            if (currentGhost != null)
                currentGhost.StopEverything();

            if (cameraAgent != null)
                cameraAgent.enabled = false;

            if (currentGhost != null && currentGhost.cinematicCameraPoint != null)
            {
                cinematicCamera.transform.SetParent(currentGhost.cinematicCameraPoint);

                float snap = 0f;
                Vector3 startPos = cinematicCamera.transform.localPosition;
                Quaternion startRot = cinematicCamera.transform.localRotation;

                while (snap < 1f)
                {
                    snap += Time.deltaTime * 2f;
                    cinematicCamera.transform.localPosition =
                        Vector3.Lerp(startPos, Vector3.zero, snap);
                    cinematicCamera.transform.localRotation =
                        Quaternion.Slerp(startRot, Quaternion.identity, snap);
                    yield return null;
                }
            }

            // ---------- PHASE 3: DESTROY ----------
            yield return new WaitForSeconds(delayBeforeDestruction);

            if (currentGhost != null)
                currentGhost.gameObject.SetActive(false);

            cinematicCamera.transform.SetParent(null);
            yield return new WaitForSeconds(timeBetweenGhosts);
        }

        Debug.Log("[Director] All ghosts destroyed.");

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(ReturnToPlayerCamera());
    }

    private IEnumerator ReturnToPlayerCamera()
    {
        Camera playerCam = playerCamera.GetComponent<Camera>();
        if (playerCam == null)
            playerCam = playerCamera.GetComponentInChildren<Camera>();

        Vector3 startPos = cinematicCamera.transform.position;
        Quaternion startRot = cinematicCamera.transform.rotation;

        Vector3 targetPos = playerCamera.position;
        Quaternion targetRot = playerCamera.rotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / returnDuration;
            cinematicCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cinematicCamera.transform.position = targetPos;
        cinematicCamera.transform.rotation = targetRot;

        cinematicCamera.enabled = false;
        cinematicCamera.gameObject.SetActive(false);

        if (playerCam != null)
            playerCam.enabled = true;

        TogglePlayerControls(true);
    }

    private void TogglePlayerControls(bool state)
    {
        Camera playerCam = playerCamera.GetComponent<Camera>();
        if (playerCam == null)
            playerCam = playerCamera.GetComponentInChildren<Camera>();

        if (playerCam != null)
            playerCam.enabled = state;

        AudioListener al = playerCamera.GetComponent<AudioListener>();
        if (al == null)
            al = playerCamera.GetComponentInChildren<AudioListener>();

        if (al != null)
            al.enabled = state;

        MonoBehaviour[] scripts = playerCamera.root.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s == this) continue;

            string n = s.GetType().Name;
            if (n.Contains("Move") || n.Contains("Look") || n.Contains("Controller") || n.Contains("Input"))
                s.enabled = state;
        }
    }
}
