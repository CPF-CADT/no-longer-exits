using System.Collections;
using UnityEngine;

public class GhostCameraController : MonoBehaviour
{
    public static GhostCameraController Instance;
    
    [Header("Settings")]
    public Transform cameraTransform; 

    // We keep these public so you can see them, but we FORCE them in Awake
    public Vector3 targetLocalPosition;
    public Vector3 targetLocalRotation;

    void Awake()
    {
        Instance = this;
        
        // --- THE FIX ---
        // We force these values right now. 
        // This stops the Inspector from using the old wrong numbers.
        targetLocalPosition = new Vector3(-0.27f, 0f, 0.36f);
        targetLocalRotation = new Vector3(0f, 90f, 90f);
        // ----------------

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    public void MoveCameraToPoint(Transform ghostModel, float duration = 0.5f)
    {
        StartCoroutine(StickToFaceRoutine(ghostModel, duration));
    }

    private IEnumerator StickToFaceRoutine(Transform ghostModel, float duration)
    {
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

        cameraTransform.SetParent(ghostModel);

        // SNAP TO EXACT VALUES
        cameraTransform.localPosition = targetLocalPosition;
        cameraTransform.localRotation = Quaternion.Euler(targetLocalRotation);
    }
}