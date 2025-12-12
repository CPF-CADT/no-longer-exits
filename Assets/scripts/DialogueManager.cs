using UnityEngine;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Objects")]
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;

    [Header("Settings")]
    public float zoomDuration = 0.5f;

    [Header("References")]
    private FirstPersonMovement playerMovement;
    private FirstPersonLook playerLook;
    private Transform mainCamera;

    // --- State Saving ---
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool isDialogueActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (Camera.main != null) mainCamera = Camera.main.transform;
        playerMovement = FindObjectOfType<FirstPersonMovement>();
        playerLook = FindObjectOfType<FirstPersonLook>();

        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    // UPDATED: Now takes the "CameraTarget" object
    public void ShowDialogue(string[] lines, float delayBetweenLines, Transform cameraTarget)
    {
        if (isDialogueActive) return;

        // 1. Lock Player
        LockPlayerAndDetachCamera();

        // 2. Move Camera to the Target Object (Just like Ghost)
        StartCoroutine(MoveCameraToTarget(cameraTarget));

        // 3. Play Text
        StartCoroutine(PlayDialogueRoutine(lines, delayBetweenLines));
    }

    private void LockPlayerAndDetachCamera()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerLook != null) playerLook.freezeCamera = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (mainCamera != null)
        {
            originalParent = mainCamera.parent;
            originalLocalPos = mainCamera.localPosition;
            originalLocalRot = mainCamera.localRotation;
            mainCamera.SetParent(null); // Detach!
        }
    }

    IEnumerator MoveCameraToTarget(Transform target)
    {
        float timer = 0f;
        Vector3 startPos = mainCamera.position;
        Quaternion startRot = mainCamera.rotation;

        while (timer < zoomDuration)
        {
            timer += Time.deltaTime;
            float t = timer / zoomDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            // Fly to the Target's position and match its rotation
            mainCamera.position = Vector3.Lerp(startPos, target.position, t);
            mainCamera.rotation = Quaternion.Slerp(startRot, target.rotation, t);
            yield return null;
        }

        // Snap to exact target values
        mainCamera.position = target.position;
        mainCamera.rotation = target.rotation;
        
        // Attach to target so camera moves with NPC animation
        mainCamera.SetParent(target);
    }

    IEnumerator PlayDialogueRoutine(string[] lines, float delay)
    {
        isDialogueActive = true;
        
        if(dialogueBox != null) dialogueBox.SetActive(true);
        if(dialogueText != null) dialogueText.enabled = true;

        foreach (string line in lines)
        {
            dialogueText.text = line;
            yield return new WaitForSeconds(delay);
        }

        CloseDialogue();
    }

    public void CloseDialogue()
    {
        if(dialogueBox != null) dialogueBox.SetActive(false);
        dialogueText.text = ""; 
        isDialogueActive = false;
        ResetPlayerAndCamera();
    }

    private void ResetPlayerAndCamera()
    {
        if (mainCamera != null && originalParent != null)
        {
            mainCamera.SetParent(originalParent);
            mainCamera.localPosition = originalLocalPos;
            mainCamera.localRotation = originalLocalRot;
        }

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerLook != null) playerLook.freezeCamera = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}