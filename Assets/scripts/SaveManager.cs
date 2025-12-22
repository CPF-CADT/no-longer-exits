using UnityEngine;
using System.IO;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string savePath;

    // We store the data temporarily here while the scene reloads
    private SaveData pendingLoadData;

    [SerializeField] private GameObject player;
    public bool autoLoadOnStart = true;
    public UnityEvent OnSaveCompleted;
    public UnityEvent OnLoadCompleted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // MUST persist between scene loads
        }
        else
        {
            Destroy(gameObject);
        }

        savePath = Application.persistentDataPath + "/horrorsave.json";
    }

    private void Start()
    {
        // If we just started the game app, try to auto-load
        if (autoLoadOnStart && File.Exists(savePath))
        {
            // We don't need to reload the scene here because the app just started
            // But we do need the delay for Unity to initialize
            StartCoroutine(InitialLoadRoutine());
        }
    }

    private IEnumerator InitialLoadRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        LoadDataInternal(); // Just load the data, scene is already fresh
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
        if (player == null) return;

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

    // --- MAIN LOAD FUNCTION ---
    public void RespawnPlayer(Transform defaultSpawn = null)
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No save found. Restarting scene fresh.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // 1. Read the file FIRST to make sure it's valid
        try
        {
            string json = File.ReadAllText(savePath);
            pendingLoadData = JsonUtility.FromJson<SaveData>(json);

            // 2. Subscribe to the scene load event
            SceneManager.sceneLoaded += OnSceneLoadedForLoad;

            // 3. Reload the Scene (This cleans the "Dirty" state)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Save file corrupted: " + ex.Message);
        }
    }

    // This runs automatically AFTER the scene finishes reloading
    private void OnSceneLoadedForLoad(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe immediately so it doesn't happen again
        SceneManager.sceneLoaded -= OnSceneLoadedForLoad;

        // Now apply the data to the fresh scene
        if (pendingLoadData != null)
        {
            ApplyLoadData(pendingLoadData);
            pendingLoadData = null; // Clear it
        }
    }

    // Re-used function for loading data without reloading scene (for Start())
    private void LoadDataInternal()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            ApplyLoadData(data);
        }
    }

    private void ApplyLoadData(SaveData data)
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
            player.transform.rotation = Quaternion.Euler(data.rotation[0], data.rotation[1], data.rotation[2]);

            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.LoadInventoryFromSave(data.inventory, data.selectedSlot, data.holdingEmpty);

        if (data.objectStates != null)
            RestoreEnvironmentStates(data.objectStates);

        OnLoadCompleted?.Invoke();
        Debug.Log("Game loaded successfully (Scene Refreshed)!");
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
        if (File.Exists(savePath)) File.Delete(savePath);
        if (reloadScene) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}