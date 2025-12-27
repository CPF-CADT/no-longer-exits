using System.Collections;
using UnityEngine;

public class GhostCameraController : MonoBehaviour
{
    public static GhostCameraController Instance;

    [Header("Settings")]
    public Transform cameraTransform;

    // Hardcoded offsets for the specific ghost model
    public Vector3 targetLocalPosition;
    public Vector3 targetLocalRotation;

    // --- NEW: Remember where the camera belongs ---
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    void Awake()
    {
        Instance = this;

        // Force specific values for ghost alignment
        targetLocalPosition = new Vector3(-0.27f, 0f, 0.36f);
        targetLocalRotation = new Vector3(0f, 90f, 90f);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // --- IMPORTANT: Save the Player as the original parent ---
        if (cameraTransform != null)
        {
            originalParent = cameraTransform.parent;
            originalLocalPos = cameraTransform.localPosition;
            originalLocalRot = cameraTransform.localRotation;
        }
    }

    public void MoveCameraToPoint(Transform ghostModel, float duration = 0.5f)
    {
        StartCoroutine(StickToFaceRoutine(ghostModel, duration));
    }

    private IEnumerator StickToFaceRoutine(Transform ghostModel, float duration)
    {
        // Detach from player so physics don't jitter the camera
        cameraTransform.SetParent(null);

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            // Calculate target relative to the GHOST MODEL
            Vector3 targetWorldPos = ghostModel.TransformPoint(targetLocalPosition);
            Quaternion targetWorldRot = ghostModel.rotation * Quaternion.Euler(targetLocalRotation);

            cameraTransform.position = Vector3.Lerp(startPos, targetWorldPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, targetWorldRot, t);

            yield return null;
        }

        // Attach to ghost so it moves with the head
        cameraTransform.SetParent(ghostModel);

        // SNAP TO EXACT VALUES
        cameraTransform.localPosition = targetLocalPosition;
        cameraTransform.localRotation = Quaternion.Euler(targetLocalRotation);
    }

    // --- THE FIXED RESET FUNCTION ---
    public void ResetCamera()
    {
        // 1. Stop the ghost scare animation immediately
        StopAllCoroutines();

        if (cameraTransform != null && originalParent != null)
        {
            // 2. DETACH from Ghost and ATTACH back to Player
            cameraTransform.SetParent(originalParent);

            // 3. Reset position to exactly where eyes should be (usually 0,0,0 relative to parent)
            cameraTransform.localPosition = originalLocalPos;
            cameraTransform.localRotation = originalLocalRot;
        }
        else
        {
            Debug.LogError("GhostCameraController: Cannot reset camera! Original parent is missing.");
        }
    }
}