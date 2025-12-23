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
    
    // Data held while scene reloads
    private SaveData pendingLoadData;

    [SerializeField] private GameObject player;
    public bool autoLoadOnStart = true;

    public UnityEvent OnSaveCompleted;
    public UnityEvent OnLoadCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "horrorsave.json");
    }

    private void Start()
    {
        if (autoLoadOnStart && File.Exists(savePath))
        {
            StartCoroutine(InitialLoadRoutine());
        }
    }

    private IEnumerator InitialLoadRoutine()
    {
        // WAIT for InventorySystem + ItemDatabase to exist
        yield return new WaitUntil(() => InventorySystem.Instance != null);
        yield return null;

        LoadDataInternal();
    }

    // -------------------- SAVE DATA CLASS --------------------
    [System.Serializable]
    public class SaveData
    {
        public float[] position;
        public float[] rotation;

        // Inventory
        public InventorySystem.InventorySlotSave[] inventory;
        public int selectedSlot;
        public bool holdingEmpty;

        // Missions (NEW)
        public int missionIndex;

        // World Objects
        public List<SaveObjectState> objectStates;
    }

    // -------------------- SAVE --------------------
    public void SaveGame()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Save failed: Player not found");
            return;
        }

        SaveData data = new SaveData
        {
            // 1. Save Player Transform
            position = new float[]
            {
                player.transform.position.x,
                player.transform.position.y,
                player.transform.position.z
            },
            rotation = new float[]
            {
                player.transform.eulerAngles.x,
                player.transform.eulerAngles.y,
                player.transform.eulerAngles.z
            },

            // 2. Save Inventory
            inventory = InventorySystem.Instance != null
                ? InventorySystem.Instance.GetSaveInventory()
                : new InventorySystem.InventorySlotSave[0],

            selectedSlot = InventorySystem.Instance != null
                ? InventorySystem.Instance.GetSelectedSlotIndex()
                : 0,

            holdingEmpty = InventorySystem.Instance == null
                || InventorySystem.Instance.GetHoldingNothing(),

            // 3. Save Mission (NEW)
            missionIndex = MissionManager.Instance != null 
                ? MissionManager.Instance.GetMissionIndex() 
                : 0,

            // 4. Save World Objects (Doors, Chests, etc.)
            objectStates = CaptureEnvironmentStates()
        };

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));

        OnSaveCompleted?.Invoke();
        Debug.Log("SAVE OK → " + savePath);
    }

    // -------------------- LOAD (SCENE REFRESH SAFE) --------------------
    public void RespawnPlayer()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No save found. Reloading fresh scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // Prevent double subscription (IMPORTANT)
        SceneManager.sceneLoaded -= OnSceneLoaded;

        string json = File.ReadAllText(savePath);
        pendingLoadData = JsonUtility.FromJson<SaveData>(json);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(ApplyLoadDelayed());
    }

    private IEnumerator ApplyLoadDelayed()
    {
        // Wait until InventorySystem exists
        yield return new WaitUntil(() => InventorySystem.Instance != null);
        yield return null; // allow ItemDatabase.OnEnable()

        ApplyLoadData(pendingLoadData);
        pendingLoadData = null;
    }

    // Used for app startup (no scene reload)
    private void LoadDataInternal()
    {
        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        StartCoroutine(ApplyLoadDelayedInternal(data));
    }

    private IEnumerator ApplyLoadDelayedInternal(SaveData data)
    {
        yield return new WaitUntil(() => InventorySystem.Instance != null);
        yield return null;
        ApplyLoadData(data);
    }

    // -------------------- APPLY DATA --------------------
    private void ApplyLoadData(SaveData data)
    {
        // 1. Load Player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            player.transform.position = new Vector3(
                data.position[0],
                data.position[1],
                data.position[2]
            );

            player.transform.rotation = Quaternion.Euler(
                data.rotation[0],
                data.rotation[1],
                data.rotation[2]
            );

            if (cc) cc.enabled = true;
            Physics.SyncTransforms();
        }

        // 2. Load Inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.LoadInventoryFromSave(
                data.inventory,
                data.selectedSlot,
                data.holdingEmpty
            );
        }

        // 3. Load Missions (NEW)
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.LoadMissionProgress(data.missionIndex);
        }

        // 4. Load World Objects
        if (data.objectStates != null)
            RestoreEnvironmentStates(data.objectStates);

        OnLoadCompleted?.Invoke();
        Debug.Log("LOAD OK → Game fully restored");
    }

    // -------------------- ENVIRONMENT HELPERS --------------------
    private List<ISaveable> GetSaveables()
    {
        return FindObjectsOfType<MonoBehaviour>(true)
            .OfType<ISaveable>()
            .ToList();
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
        var dict = new Dictionary<string, SaveObjectState>();

        foreach (var s in states)
        {
            if (!string.IsNullOrEmpty(s.id) && !dict.ContainsKey(s.id))
                dict.Add(s.id, s);
        }

        foreach (var obj in GetSaveables())
        {
            string id = obj.GetUniqueID();
            if (!string.IsNullOrEmpty(id) && dict.TryGetValue(id, out var state))
                obj.RestoreState(state);
        }
    }

    // -------------------- DELETE --------------------
    public void DeleteSave(bool reloadScene = false)
    {
        if (File.Exists(savePath))
            File.Delete(savePath);

        if (reloadScene)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}