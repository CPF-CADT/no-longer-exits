using UnityEngine;

public class StonePuzzle : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public ItemData stoneItem;      // The stone item to check
    public GameObject puzzleCanvas;  // The canvas to activate
    public int requiredStones = 4;  // Number of stones needed

    private bool puzzleActivated = false;

    private void Start()
    {
        if (puzzleCanvas != null)
            puzzleCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null || stoneItem == null) return;

        // Count how many stones of the required type the player has
        var slots = inventory.GetSaveInventory();
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].itemUniqueID == stoneItem.uniqueID)
                count++;
        }

        if (!puzzleActivated && count >= requiredStones)
        {
            // Remove required stones
            int removed = 0;
            for (int i = 0; i < slots.Length && removed < requiredStones; i++)
            {
                if (slots[i] != null && slots[i].itemUniqueID == stoneItem.uniqueID)
                {
                    inventory.SelectSlot(i);         // Set current slot
                    inventory.RemoveCurrentItem();  // Remove it
                    removed++;
                }
            }

            // Activate puzzle
            puzzleCanvas.SetActive(true);
            puzzleActivated = true;
            Debug.Log("Puzzle Activated! Stones consumed.");
        }
        else if (!puzzleActivated)
        {
            Debug.Log("Not enough stones to activate the puzzle.");
        }
        else
        {
            // Puzzle already activated, toggle canvas freely
            puzzleCanvas.SetActive(!puzzleCanvas.activeSelf);
        }
    }
}
