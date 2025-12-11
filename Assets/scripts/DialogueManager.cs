using UnityEngine;
using TMPro; // Needed for Text Mesh Pro
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Assign Specific Objects Here")]
    // ONLY drag the background image of the text box here. 
    // Do NOT drag the whole Canvas here.
    public GameObject dialogueBox; 
    
    // Drag your TextMeshPro object here
    public TextMeshProUGUI dialogueText;

    private bool isDialogueActive = false;

    void Awake()
    {
        // Singleton Setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Safety Check: Only hide the box if it is assigned
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false); 
        }
    }

    public void ShowDialogue(string[] lines, float delayBetweenLines)
    {
        if (isDialogueActive) return;

        StartCoroutine(PlayDialogueRoutine(lines, delayBetweenLines));
    }

    IEnumerator PlayDialogueRoutine(string[] lines, float delay)
    {
        isDialogueActive = true;
        
        // Show only the text box
        if(dialogueBox != null) dialogueBox.SetActive(true);

        foreach (string line in lines)
        {
            dialogueText.text = line;
            yield return new WaitForSeconds(delay);
        }

        // Hide only the text box
        if(dialogueBox != null) dialogueBox.SetActive(false);
        
        // Clear text so it doesn't linger
        dialogueText.text = ""; 
        isDialogueActive = false;
    }
}