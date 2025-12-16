using UnityEngine;

public class SaveLoadUI : MonoBehaviour
{
    [Tooltip("Press F5 to Save, F9 to Load/Respawn")]
    public bool enableHotkeys = true;

    [Header("Hotkeys")]
    [Tooltip("Deletes save and reloads the current scene")]
    public KeyCode resetKey = KeyCode.F10;

    void Update()
    {
        if (!enableHotkeys) return;
        if (Input.GetKeyDown(KeyCode.F5)) Save();
        if (Input.GetKeyDown(KeyCode.F9)) Load();
        if (Input.GetKeyDown(resetKey)) ResetGame();
    }

    public void Save()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        else Debug.LogWarning("SaveManager.Instance is null. Ensure SaveManager exists in scene.");
    }

    public void Load()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.RespawnPlayer();
        else Debug.LogWarning("SaveManager.Instance is null. Ensure SaveManager exists in scene.");
    }

    public void ResetGame()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.DeleteSave(true);
        else Debug.LogWarning("SaveManager.Instance is null. Ensure SaveManager exists in scene.");
    }
}
