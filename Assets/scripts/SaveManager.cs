using UnityEngine;
using System.IO;
using UnityEngine.Events;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string savePath;
    private GameObject player;

    public bool autoLoadOnStart = true;
    public UnityEvent OnSaveCompleted;
    public UnityEvent OnLoadCompleted;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        savePath = Application.persistentDataPath + "/horrorsave.json";
    }

    private void Start()
    {
        if (autoLoadOnStart && File.Exists(savePath)) Invoke(nameof(RespawnPlayer), 0.01f);
    }

    [System.Serializable]
    public class SaveData
    {
        public float[] position;
        public float[] rotation;
        public InventorySystem.InventorySlotSave[] inventory;
        public int selectedSlot;
        public bool holdingEmpty;
    }

    public void SaveGame()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogError("Cannot Save: Player not found!"); return; }

        SaveData data = new SaveData
        {
            position = new float[] { player.transform.position.x, player.transform.position.y, player.transform.position.z },
            rotation = new float[] { player.transform.eulerAngles.x, player.transform.eulerAngles.y, player.transform.eulerAngles.z },
            inventory = InventorySystem.Instance?.GetSaveInventory(),
            selectedSlot = InventorySystem.Instance?.GetSelectedSlotIndex() ?? 0,
            holdingEmpty = InventorySystem.Instance?.GetHoldingNothing() ?? true
        };

        File.WriteAllText(savePath, JsonUtility.ToJson(data));
        OnSaveCompleted?.Invoke();
        Debug.Log("Game saved!");
    }

    public void RespawnPlayer()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) return;

        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        Vector3 pos = new Vector3(data.position[0], data.position[1], data.position[2]);
        Vector3 rot = new Vector3(data.rotation[0], data.rotation[1], data.rotation[2]);

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = pos;
        player.transform.eulerAngles = rot;
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        InventorySystem.Instance?.LoadInventoryFromSave(data.inventory, data.selectedSlot, data.holdingEmpty);

        OnLoadCompleted?.Invoke();
        Debug.Log("Game loaded!");
    }
}
