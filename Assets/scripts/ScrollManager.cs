using UnityEngine;
using UnityEngine.UI; // Needed for UI

public class ScrollManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiPanel;   // Drag your "ScrollCanvas" or Panel here
    public Image displayImage;   // Drag the "StoryDisplay" Image here

    // Singleton (This allows other scripts to find this one easily)
    public static ScrollManager Instance;

    void Awake()
    {
        Instance = this;
        if (uiPanel != null) uiPanel.SetActive(false); // Hide at start
    }

    void Update()
    {
        // Press F or Escape to close
        if (uiPanel.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)))
        {
            CloseScroll();
        }
    }

    public void OpenScroll(Sprite img)
    {
        if (uiPanel == null || displayImage == null) return;

        displayImage.sprite = img; // Swap the image
        displayImage.preserveAspect = true; // Keep image ratio
        uiPanel.SetActive(true);   // Show UI

        // Optional: Unlock cursor so you can see it
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseScroll()
    {
        if (uiPanel == null) return;

        uiPanel.SetActive(false); // Hide UI

        // Optional: Lock cursor again for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}