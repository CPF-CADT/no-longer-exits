using UnityEngine;
using System.IO;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq; // Added for easier list handling

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string savePath;
    
    // Serialized so you can drag it in manually if Tag lookup fails
    [SerializeField] private GameObject player; 

    public bool autoLoadOnStart = true;
    public UnityEvent OnSaveCompleted;
    public UnityEvent OnLoadCompleted;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else 
        { 
            Destroy(gameObject); 
        }

        savePath = Application.persistentDataPath + "/horrorsave.json";
    }

    private void Start()
    {
        // Small delay to ensure all other Awake/Start methods have finished
        if (autoLoadOnStart && File.Exists(savePath)) 
            Invoke(nameof(RespawnPlayer), 0.1f); 
    }

    [System.Serializable]
    public class SaveData
    {
        public float[] position;
        public float[] rotation;
        public InventorySystem.InventorySlotSave[] inventory;
        public int selectedSlot;
        public bool holdingEmpty;
        public List<SaveObjectState> objectStates; 
    }

    public void SaveGame()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { Debug.LogError("Cannot Save: Player not found!"); return; }

        SaveData data = new SaveData
        {
            position = new float[] { player.transform.position.x, player.transform.position.y, player.transform.position.z },
            rotation = new float[] { player.transform.eulerAngles.x, player.transform.eulerAngles.y, player.transform.eulerAngles.z },
            
            // Null checks added for safety
            inventory = InventorySystem.Instance != null ? InventorySystem.Instance.GetSaveInventory() : new InventorySystem.InventorySlotSave[0],
            selectedSlot = InventorySystem.Instance != null ? InventorySystem.Instance.GetSelectedSlotIndex() : 0,
            holdingEmpty = InventorySystem.Instance == null || InventorySystem.Instance.GetHoldingNothing(),
            
            objectStates = CaptureEnvironmentStates()
        };

        string json = JsonUtility.ToJson(data, true); // 'true' makes the JSON pretty-print (easier to read for debugging)
        File.WriteAllText(savePath, json);
        
        OnSaveCompleted?.Invoke();
        Debug.Log($"Game saved to: {savePath}");
    }

    public void RespawnPlayer()
    {
        if (!File.Exists(savePath)) 
        {
            Debug.Log("No save file found.");
            return;
        }

        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        try 
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 1. Move Player
            Vector3 pos = new Vector3(data.position[0], data.position[1], data.position[2]);
            Vector3 rot = new Vector3(data.rotation[0], data.rotation[1], data.rotation[2]);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // Must disable CC to teleport

            player.transform.position = pos;
            player.transform.eulerAngles = rot;
            Physics.SyncTransforms(); // Force physics update

            if (cc != null) cc.enabled = true;

            // 2. Load Inventory
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.LoadInventoryFromSave(data.inventory, data.selectedSlot, data.holdingEmpty);
            }

            // 3. Load Environment (Doors, Chests, Puzzles)
            if (data.objectStates != null && data.objectStates.Count > 0)
            {
                RestoreEnvironmentStates(data.objectStates);
            }

            OnLoadCompleted?.Invoke();
            Debug.Log("Game loaded successfully!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load game: {ex.Message}");
        }
    }

    // -------------------- Environment Save/Load --------------------
    private List<ISaveable> GetSaveables()
    {
        // FindObjectsOfType(true) includes inactive objects, which is good.
        // We use Linq to filter for ISaveable interfaces quickly.
        return FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
    }

    private List<SaveObjectState> CaptureEnvironmentStates()
    {
        var list = new List<SaveObjectState>();
        foreach (var s in GetSaveables())
        {
            var state = s.CaptureState();
            // Only save if the ID is valid
            if (state != null && !string.IsNullOrEmpty(state.id))
                list.Add(state);
            else
                Debug.LogWarning($"[SaveManager] Found saveable object without a valid ID. It will not be saved.");
        }
        return list;
    }

    private void RestoreEnvironmentStates(List<SaveObjectState> states)
    {
        // Convert list to Dictionary for fast lookup
        var stateDict = new Dictionary<string, SaveObjectState>();
        foreach (var s in states)
        {
            if (!string.IsNullOrEmpty(s.id) && !stateDict.ContainsKey(s.id))
                stateDict.Add(s.id, s);
        }

        // Find all saveable objects currently in the scene
        foreach (var saveableObj in GetSaveables())
        {
            string id = saveableObj.GetUniqueID();
            
            // If this object exists in our save file, restore it
            if (!string.IsNullOrEmpty(id) && stateDict.TryGetValue(id, out var savedState))
            {
                saveableObj.RestoreState(savedState);
            }
        }
    }

    public void DeleteSave(bool reloadScene = false)
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save file deleted.");
        }

        if (reloadScene)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}