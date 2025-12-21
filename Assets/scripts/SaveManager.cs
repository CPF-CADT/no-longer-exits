using UnityEngine;
using System.IO;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq; // For easier list handling

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string savePath;

    [SerializeField] private GameObject player; // Player reference if Tag lookup fails
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
            inventory = InventorySystem.Instance != null ? InventorySystem.Instance.GetSaveInventory() : new InventorySystem.InventorySlotSave[0],
            selectedSlot = InventorySystem.Instance != null ? InventorySystem.Instance.GetSelectedSlotIndex() : 0,
            holdingEmpty = InventorySystem.Instance == null || InventorySystem.Instance.GetHoldingNothing(),
            objectStates = CaptureEnvironmentStates()
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        OnSaveCompleted?.Invoke();
        Debug.Log($"Game saved to: {savePath}");
    }

    public void RespawnPlayer(Transform defaultSpawn = null)
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (!File.Exists(savePath))
        {
            // No save found, reload scene
            Debug.Log("No save file found. Reloading scene...");

            if (defaultSpawn != null)
            {
                // Move player to default spawn after scene reload
                SceneManager.sceneLoaded += (scene, mode) =>
                {
                    if (player != null)
                    {
                        CharacterController cc = player.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;

                        player.transform.position = defaultSpawn.position;
                        player.transform.rotation = defaultSpawn.rotation;

                        if (cc != null) cc.enabled = true;

                        Physics.SyncTransforms();
                    }
                };
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // Save exists, load normally
        try
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            Vector3 pos = new Vector3(data.position[0], data.position[1], data.position[2]);
            Quaternion rot = Quaternion.Euler(data.rotation[0], data.rotation[1], data.rotation[2]);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = pos;
            player.transform.rotation = rot;

            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();

            // Load inventory
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.LoadInventoryFromSave(data.inventory, data.selectedSlot, data.holdingEmpty);

            // Restore environment
            if (data.objectStates != null && data.objectStates.Count > 0)
                RestoreEnvironmentStates(data.objectStates);

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
        return FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
    }

    private List<SaveObjectState> CaptureEnvironmentStates()
    {
        var list = new List<SaveObjectState>();
        foreach (var s in GetSaveables())
        {
            var state = s.CaptureState();
            if (state != null && !string.IsNullOrEmpty(state.id))
                list.Add(state);
        }
        return list;
    }

    private void RestoreEnvironmentStates(List<SaveObjectState> states)
    {
        var stateDict = new Dictionary<string, SaveObjectState>();
        foreach (var s in states)
        {
            if (!string.IsNullOrEmpty(s.id) && !stateDict.ContainsKey(s.id))
                stateDict.Add(s.id, s);
        }

        foreach (var saveableObj in GetSaveables())
        {
            string id = saveableObj.GetUniqueID();
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
