using UnityEngine;

public class WeaponSocket : MonoBehaviour, ISaveable
{
    // ================= SETTINGS =================
    [Header("Settings")]
    public VishnuWeaponType requiredWeapon;

    // ================= PUZZLE =================
    [Header("Puzzle")]
    [SerializeField] private PersistentID puzzleManagerID;
    private PuzzleManager puzzleManager;

    // ================= STATE =================
    [Header("State")]
    public VishnuWeaponItemData currentWeapon;
    private GameObject currentWeaponModel;

    public bool isOccupied => currentWeapon != null;

    // ================= COMPONENTS =================
    private Collider socketCollider;

    // ================= SAVE =================
    [Header("Persistence")]
    public PersistentID persistentID;

    // ================= FALLBACK =================
    [Header("Fallback (Optional)")]
    public VishnuWeaponItemData fallbackWeapon;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Awake()
    {
        socketCollider = GetComponent<Collider>();
        if (socketCollider == null)
            Debug.LogWarning("[WeaponSocket] Missing Collider");

        if (persistentID == null)
        {
            persistentID = GetComponent<PersistentID>();
            if (persistentID == null)
                persistentID = gameObject.AddComponent<PersistentID>();
        }
    }

    private void Start()
    {
        ResolvePuzzleManager();
    }

    // =====================================================
    // INTERACTION
    // =====================================================

    public void Interact(InventorySystem inventory)
    {
        if (inventory == null) return;

        var currentItem = inventory.GetCurrentItem();
        var weaponInHand = currentItem as VishnuWeaponItemData;

        // ---- PICK UP FROM SOCKET ----
        if (isOccupied && weaponInHand == null)
        {
            var retrieved = TakeWeapon();
            if (retrieved != null)
                inventory.AddItem(retrieved);
            return;
        }

        // ---- NOTHING TO PLACE ----
        if (weaponInHand == null) return;

        // ---- SWAP ----
        var oldWeapon = TakeWeapon();
        if (oldWeapon != null)
            inventory.AddItem(oldWeapon);

        // ---- PLACE ----
        TryPlaceWeapon(weaponInHand);

        if (currentWeapon == weaponInHand)
            inventory.ConsumeCurrentItem();
    }

    // =====================================================
    // RUNTIME PLACEMENT
    // =====================================================

    public VishnuWeaponItemData TryPlaceWeapon(VishnuWeaponItemData weapon)
    {
        if (weapon == null) return null;

        SpawnModel(weapon, true);
        return null;
    }

    public VishnuWeaponItemData TakeWeapon()
    {
        if (!isOccupied) return null;

        if (currentWeapon.weaponType == requiredWeapon)
        {
            ResolvePuzzleManager();
            puzzleManager?.NotifyWeaponRemoved();
        }

        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        var result = currentWeapon;
        currentWeapon = null;
        currentWeaponModel = null;

        return result;
    }

    // =====================================================
    // MODEL SPAWN (SILENT OR NOT)
    // =====================================================

    private void SpawnModel(VishnuWeaponItemData weapon, bool notifyPuzzle)
    {
        if (weapon == null || weapon.model == null) return;

        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentWeapon = weapon;
        currentWeaponModel = Instantiate(weapon.model, transform);

        currentWeaponModel.transform.localPosition = Vector3.zero;
        currentWeaponModel.transform.localRotation = weapon.model.transform.localRotation;
        currentWeaponModel.transform.localScale = weapon.model.transform.localScale;

        foreach (var p in currentWeaponModel.GetComponentsInChildren<ItemPickup>())
            Destroy(p);
        foreach (var c in currentWeaponModel.GetComponentsInChildren<Collider>())
            Destroy(c);
        foreach (var r in currentWeaponModel.GetComponentsInChildren<Rigidbody>())
            Destroy(r);

        currentWeaponModel.SetActive(true);

        if (notifyPuzzle && weapon.weaponType == requiredWeapon)
        {
            ResolvePuzzleManager();
            puzzleManager?.NotifyWeaponPlaced();
        }
    }

    // =====================================================
    // SAVE / LOAD
    // =====================================================

    public string GetUniqueID()
    {
        return persistentID != null
            ? persistentID.id
            : gameObject.GetInstanceID().ToString();
    }

    public SaveObjectState CaptureState()
    {
        return new SaveObjectState
        {
            id = GetUniqueID(),
            type = "WeaponSocket",
            socketWeaponID = currentWeapon ? currentWeapon.UniqueID : null,
            socketWeaponName = currentWeapon ? currentWeapon.itemName : null
        };
    }

    public void RestoreState(SaveObjectState state)
    {
        if (state == null || state.type != "WeaponSocket") return;

        ClearSocket();

        if (string.IsNullOrEmpty(state.socketWeaponID))
            return;

        var weapon = ResolveWeapon(state.socketWeaponID, state.socketWeaponName)
                     ?? fallbackWeapon;

        if (weapon != null)
            SpawnModel(weapon, false); // 🔴 NO puzzle notify
    }

    private void ClearSocket()
    {
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentWeapon = null;
        currentWeaponModel = null;
    }

    // =====================================================
    // WEAPON RESOLUTION
    // =====================================================

    private VishnuWeaponItemData ResolveWeapon(string id, string name)
    {
        ItemData item = null;

        if (!string.IsNullOrEmpty(id))
        {
            if (ItemRegistry.Instance != null)
                item = ItemRegistry.Instance.FindByUniqueID(id);

            if (item == null && InventorySystem.Instance?.itemDatabase != null)
                item = InventorySystem.Instance.itemDatabase.GetItemByID(id);
        }

        if (item == null && !string.IsNullOrEmpty(name))
        {
            if (ItemRegistry.Instance != null)
                item = ItemRegistry.Instance.FindByName(name);
        }

        return item as VishnuWeaponItemData ?? FindByWeaponType(requiredWeapon);
    }

    private VishnuWeaponItemData FindByWeaponType(VishnuWeaponType type)
    {
        if (ItemRegistry.Instance?.items != null)
        {
            foreach (var it in ItemRegistry.Instance.items)
                if (it is VishnuWeaponItemData vw && vw.weaponType == type)
                    return vw;
        }

        if (InventorySystem.Instance?.itemDatabase?.allItems != null)
        {
            foreach (var it in InventorySystem.Instance.itemDatabase.allItems)
                if (it is VishnuWeaponItemData vw && vw.weaponType == type)
                    return vw;
        }

        return fallbackWeapon != null && fallbackWeapon.weaponType == type
            ? fallbackWeapon
            : null;
    }

    // =====================================================
    // PUZZLE MANAGER RESOLUTION
    // =====================================================

    private void ResolvePuzzleManager()
    {
        // First, try to find PuzzleManager by saved ID
        if (!string.IsNullOrEmpty(puzzleManagerID.id))
        {
            puzzleManager = FindPuzzleManagerByID(puzzleManagerID.id);
        }

        // Fallback: find any PuzzleManager in scene
        if (puzzleManager == null)
        {
            puzzleManager = FindObjectOfType<PuzzleManager>();
            if (puzzleManager != null && puzzleManager.persistentID != null)
            {
                // Automatically set the puzzleManagerID from the PuzzleManager in scene
                puzzleManagerID = puzzleManager.persistentID;
            }
        }

        if (puzzleManager == null)
            Debug.LogError($"[WeaponSocket] PuzzleManager NOT FOUND for '{name}'");
    }

    private PuzzleManager FindPuzzleManagerByID(string id)
    {
        var all = FindObjectsOfType<PuzzleManager>(true);
        foreach (var pm in all)
            if (pm.persistentID != null && pm.persistentID.id == id)
                return pm;
        return null;
    }
}
