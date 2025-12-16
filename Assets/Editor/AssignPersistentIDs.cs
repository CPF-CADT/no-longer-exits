#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Collections.Generic;

public static class AssignPersistentIDs
{
    [MenuItem("Tools/Persistence/Assign Persistent IDs to Scene Objects")] 
    public static void AssignIDsToSaveables()
    {
        var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);
        int processed = 0;
        int added = 0;
        int setMissing = 0;

        var uniqueSet = new HashSet<string>();

        foreach (var b in behaviours)
        {
            if (b is ISaveable)
            {
                var go = b.gameObject;
                var pid = go.GetComponent<PersistentID>();
                if (pid == null)
                {
                    pid = go.AddComponent<PersistentID>();
                    added++;
                }

                if (string.IsNullOrEmpty(pid.id))
                {
                    pid.id = Guid.NewGuid().ToString();
                    setMissing++;
                }

                // Ensure uniqueness (rare edge cases if duplicated by copy/paste)
                if (uniqueSet.Contains(pid.id))
                {
                    pid.id = Guid.NewGuid().ToString();
                    setMissing++;
                }
                uniqueSet.Add(pid.id);

                EditorUtility.SetDirty(pid);
                processed++;
            }
        }

        if (processed > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
        }

        Debug.Log($"PersistentID assignment complete. Processed: {processed}, Added: {added}, Fixed IDs: {setMissing}");
    }
}
#endif
