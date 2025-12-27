using UnityEngine;

public class SaveStation : MonoBehaviour
{
    public void Interact()
    {
        // Just tell the manager to save the player's current state
        SaveManager.Instance.SaveGame();
        
        Debug.Log("Save Station Used.");
    }
}