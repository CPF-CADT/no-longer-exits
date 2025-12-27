using UnityEngine;

public class StonePuzzle : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public ItemData stoneItem;       // The stone item to check
    public GameObject puzzleCanvas;  // The canvas to activate
    public int requiredStones = 4;   // Number of stones needed

    private bool puzzleActivated = false;
    private bool puzzleCompleted = false; // <-- new flag

    private void Start()
    {
        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (puzzleCompleted)
        {
            Debug.Log("Puzzle already completed, cannot play again.");
            return; // prevent reactivation
        }

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null || stoneItem == null) return;

        // Count how many stones of the required type the player has
        var slots = inventory.GetSaveInventory();
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].itemUniqueID == stoneItem.UniqueID)
                count++;
        }

        if (!puzzleActivated && count >= requiredStones)
        {
            // Remove required stones
            int removed = 0;
            for (int i = 0; i < slots.Length && removed < requiredStones; i++)
            {
                if (slots[i] != null && slots[i].itemUniqueID == stoneItem.UniqueID)
                {
                    inventory.SelectSlot(i);         // Set current slot
                    inventory.RemoveCurrentItem();  // Remove it
                    removed++;
                }
            }

            // Activate puzzle and unlock mouse
            ActivatePuzzle();
            Debug.Log("Puzzle Activated! Stones consumed.");
        }
        else if (!puzzleActivated)
        {
            Debug.Log("Not enough stones to activate the puzzle.");
        }
        else
        {
            // Puzzle already activated, toggle canvas freely
            TogglePuzzleCanvas();
        }
    }

    // ------------------ PUZZLE ACTIVATION ------------------

    private void ActivatePuzzle()
    {
        if (puzzleCanvas == null) return;

        puzzleCanvas.SetActive(true);
        puzzleActivated = true;

        // Unlock mouse cursor so player can interact
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Optional: pause player movement
        // PlayerController.Instance.enabled = false;
    }

    private void TogglePuzzleCanvas()
    {
        if (puzzleCanvas == null) return;

        bool isActive = !puzzleCanvas.activeSelf;
        puzzleCanvas.SetActive(isActive);

        if (isActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ------------------ CLOSE PUZZLE MANUALLY ------------------
    public void ClosePuzzle()
    {
        if (puzzleCanvas == null) return;

        puzzleCanvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ------------------ PUZZLE WIN ------------------
    public void OnPuzzleWin()
    {
        if (puzzleCompleted) return;

        puzzleCompleted = true; // <-- mark puzzle as completed

        // Hide puzzle canvas
        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(false);

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Puzzle solved! Canvas hidden. Cannot play again.");
    }
}
