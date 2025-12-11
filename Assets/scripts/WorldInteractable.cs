using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WorldInteractable : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject hintCanvasObject;   // Hint canvas
    public Image keyIcon;                 // Optional
    public TextMeshProUGUI actionText;    // Optional

    [Header("Data")]
    public Sprite keySprite;              // Optional
    public string actionDescription = ""; // Optional text

    void Start()
    {
        // Set icon only if both are assigned
        if (keyIcon != null)
        {
            if (keySprite != null)
            {
                keyIcon.sprite = keySprite;
                keyIcon.enabled = true;
            }
            else
            {
                keyIcon.enabled = false; // No image? Hide the icon.
            }
        }

        // Set action text if available
        if (actionText != null)
        {
            if (!string.IsNullOrEmpty(actionDescription))
            {
                actionText.text = actionDescription;
                actionText.enabled = true;
            }
            else
            {
                actionText.enabled = false; // No text? Hide it.
            }
        }

        // Hide the UI by default
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
