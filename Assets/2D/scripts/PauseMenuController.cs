using UnityEngine;
using UnityEngine.SceneManagement; // Needed for loading scenes

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI; // Assign your Panel here

    [Header("Settings")]
    public string startMenuSceneName = "StartMenu"; // Exact name of your start menu scene
    public static bool isPaused = false;

    void Update()
    {
        // Check for ESC key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Hide UI
        Time.timeScale = 1f;          // Unfreeze game
        isPaused = false;

        // Lock cursor back to center (for FPS/TPS games)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);  // Show UI
        Time.timeScale = 0f;          // Freeze game (stops movement)
        isPaused = true;

        // Unlock cursor so you can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadMainMenu()
    {
        // IMPORTANT: Always reset time before loading a new scene
        Time.timeScale = 1f; 
        SceneManager.LoadScene(startMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stops play mode in editor
#endif
    }
}