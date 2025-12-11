using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    
    private string savePath;
    private GameObject player; // Cached reference

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        savePath = Application.persistentDataPath + "/horrorsave.json";
    }

    // --- FIX 1: SAVE THE PLAYER'S EXACT POSITION, NOT THE STATION ---
    public void SaveGame()
    {
        // Find player if we don't have it
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogError("Cannot Save: Player not found!"); return; }

        SaveData data = new SaveData();

        // Record the PLAYER'S current transform
        Vector3 pPos = player.transform.position;
        Vector3 pRot = player.transform.eulerAngles;

        data.position = new float[] { pPos.x, pPos.y, pPos.z };
        data.rotation = new float[] { pRot.x, pRot.y, pRot.z };

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(savePath, json);

        Debug.Log("GAME SAVED: " + pPos);
    }

    // --- FIX 2: ROBUST RESPAWN LOGIC ---
    public void RespawnPlayer()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) return;

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            Vector3 loadPos = new Vector3(data.position[0], data.position[1], data.position[2]);
            Vector3 loadRot = new Vector3(data.rotation[0], data.rotation[1], data.rotation[2]);

            // DISABLE CONTROLLER TO ALLOW TELEPORT
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // TELEPORT
            player.transform.position = loadPos;
            player.transform.eulerAngles = loadRot;
            Physics.SyncTransforms();

            // RE-ENABLE CONTROLLER
            if (cc != null) cc.enabled = true;
            
            Debug.Log("RESPAWNED at saved location.");
        }
        else
        {
            Debug.LogWarning("No Save Found. Reloading Scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

[System.Serializable]
public class SaveData
{
    public float[] position;
    public float[] rotation;
}