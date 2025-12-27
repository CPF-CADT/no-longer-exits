using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WorldInteractable : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject hintCanvasObject;   // The background canvas for the hint
    public Image keyIcon;                 
    public TextMeshProUGUI actionText;    // Drag your Hint Text here (NOT Dialogue Text)

    [Header("Data")]
    public Sprite keySprite;              
    public string actionDescription = ""; 

    void Start()
    {
        // 1. Handle the Icon
        if (keyIcon != null)
        {
            if (keySprite != null)
            {
                keyIcon.sprite = keySprite;
                keyIcon.enabled = true;
            }
            else
            {
                keyIcon.enabled = false; 
            }
        }

        // 2. Handle the Text (FIXED VERSION)
        if (actionText != null)
        {
            if (!string.IsNullOrEmpty(actionDescription))
            {
                actionText.text = actionDescription;
                // We do NOT toggle .enabled here to avoid breaking shared texts
            }
            else
            {
                actionText.text = ""; // Just clear the text, don't disable the component
            }
        }

        // 3. Hide the whole Hint Canvas by default
        if (hintCanvasObject != null)
            hintCanvasObject.SetActive(false);
    }

    public void ShowHint()
    {
        if (hintCanvasObject != null)
            hintCanvasObject.SetActive(true);
    }

    public void HideHint()
    {
        if (hintCanvasObject != null)
            hintCanvasObject.SetActive(false);
    }
}