using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public string quickSave = "quick.json";
    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }
    public void LoadGame()
    {
        GameData.ShouldLoadSave = true; // tell Game scene to load save
        SceneManager.LoadScene("Game");
    }

    public void StartNewGame()
    {
        GameData.ShouldLoadSave = false; // start fresh
        SceneManager.LoadScene("Game");
    }


    public void QuickGame()
    {
        string path = Path.Combine(Application.persistentDataPath, quickSave);

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveManager] Deleted save file: {path}");
        }
        else
        {
            Debug.Log("[SaveManager] No quick save found to delete.");
        }

        SceneManager.LoadScene("Quick");
    }
}
