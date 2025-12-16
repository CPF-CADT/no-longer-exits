#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class DeleteSaveMenu
{
    [MenuItem("Tools/Persistence/Delete Save + Reload %#r")] // Ctrl/Cmd + Shift + R
    public static void DeleteSaveAndReload()
    {
        string path = Application.persistentDataPath + "/horrorsave.json";

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"Deleted save file at: {path}");
            }
            else
            {
                Debug.Log("No save file found to delete.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to delete save: {ex.Message}");
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManagerHelpers.ReloadCurrentScene();
        }
    }
}

public static class EditorSceneManagerHelpers
{
    public static void ReloadCurrentScene()
    {
#if UNITY_EDITOR
        var active = SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(active.path);
#endif
    }
}
#endif
