using UnityEngine;

public class SaveLoadUI : MonoBehaviour
{
    [Tooltip("Press F5 to Save, F9 to Load/Respawn")]
    public bool enableHotkeys = true;

    void Update()
    {
        if (!enableHotkeys) return;
        if (Input.GetKeyDown(KeyCode.F5)) Save();
        if (Input.GetKeyDown(KeyCode.F9)) Load();
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
}
