using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [TextArea(3, 10)] // Gives you a bigger box to type in Inspector
    public string[] sentences;
    
    public float delayBetweenLines = 3f; // How long to read each line

    public void Interact()
    {
        // Call the manager to start the dialogue
        DialogueManager.Instance.ShowDialogue(sentences, delayBetweenLines);
    }
}