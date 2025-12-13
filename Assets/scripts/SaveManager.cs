using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string savePath;
    private GameObject player; // Cached reference
    [Header("Auto Load")]
    public bool autoLoadOnStart = true;

    [Header("Events")]
    public UnityEvent OnSaveCompleted;
    public UnityEvent OnLoadCompleted;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        savePath = Application.persistentDataPath + "/horrorsave.json";
        Debug.Log(SaveManager.Instance != null ? SaveManager.Instance : "no instance");
        Debug.Log(savePath);
    }

    private void Start()
    {
        if (autoLoadOnStart)
        {
            // Only attempt auto-load if a save file exists; otherwise skip silently
            if (File.Exists(savePath))
            {
                // Delay one frame to ensure player and scene objects are initialized
                Invoke(nameof(RespawnPlayer), 0.01f);
            }
            else
            {
                Debug.Log("No save found on start — skipping auto-load.");
            }
        }
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

        if (InventorySystem.Instance != null)
        {
            data.inventory = InventorySystem.Instance.GetSaveInventory();
            data.selectedSlot = InventorySystem.Instance.GetSelectedSlotIndex();
            data.holdingEmpty = InventorySystem.Instance.GetHoldingNothing();
        }

        string json = JsonUtility.ToJson(data);
        // Ensure directory exists (should for persistentDataPath, but be defensive)
        try
        {
            string dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        catch { }

        File.WriteAllText(savePath, json);

        Debug.Log("GAME SAVED: " + pPos);
        OnSaveCompleted?.Invoke();
    }

    // --- FIX 2: ROBUST RESPAWN LOGIC ---
    public void RespawnPlayer()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) return;

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = null;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Failed to parse save file: " + ex.Message);
            }

            if (data == null || data.position == null || data.position.Length < 3 || data.rotation == null || data.rotation.Length < 3)
            {
                Debug.LogWarning("Save file is corrupted or incomplete. Backing up corrupted save and skipping load.");
                try
                {
                    string backupPath = savePath + ".bak";
                    if (File.Exists(backupPath))
                    {
                        backupPath = savePath + ".bak." + System.DateTime.Now.ToString("yyyyMMddHHmmss");
                    }
                    File.Move(savePath, backupPath);
                    Debug.Log("Corrupted save moved to: " + backupPath);
                }
                catch (System.Exception moveEx)
                {
                    Debug.LogWarning("Failed to backup corrupted save: " + moveEx.Message);
                }
                return;
            }

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

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.LoadInventoryFromNames(data.inventory, data.selectedSlot, data.holdingEmpty);
            }
            OnLoadCompleted?.Invoke();
        }
        else
        {
            // If there's no save file, do not reload the scene — just skip load.
            Debug.Log("No save file found — skipping respawn/load.");
        }
    }
}

[System.Serializable]
public class SaveData
{
    public float[] position;
    public float[] rotation;
    public string[] inventory;
    public int selectedSlot;
    public bool holdingEmpty;
}