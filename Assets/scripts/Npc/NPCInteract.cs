using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [TextArea(3, 10)]
    public string[] sentences;
    public float delayBetweenLines = 3f;

    [Header("Camera Setup")]
    // DRAG THE 'CameraTarget' CHILD OBJECT HERE
    public Transform cameraViewPoint; 

    public void Interact()
    {
        // Safety Check
        if (cameraViewPoint == null)
        {
            Debug.LogError("You forgot to assign the Camera View Point on " + gameObject.name);
            return;
        }

        DialogueManager.Instance.ShowDialogue(sentences, delayBetweenLines, cameraViewPoint);
    }
}