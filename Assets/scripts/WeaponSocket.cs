using UnityEngine;

public class WeaponSocket : MonoBehaviour, ISaveable
{
    [Header("Settings")]
    public VishnuWeaponType requiredWeapon;
    public PuzzleManager puzzleManager;

    [Header("State")]
    public VishnuWeaponItemData currentWeapon;       // Weapon currently in socket
    private GameObject currentWeaponModel;           // Reference to instantiated model

    public bool isOccupied => currentWeapon != null;

    private Collider socketCollider;
    [Header("Persistence")]
    public PersistentID persistentID;

    [Header("Fallback (Optional)")]
    public VishnuWeaponItemData fallbackWeapon;

    private void Awake()
    {
        socketCollider = GetComponent<Collider>();
        if (socketCollider == null)
            Debug.LogWarning("WeaponSocket requires a Collider on the same GameObject for interaction.");

        if (persistentID == null)
        {
            persistentID = GetComponent<PersistentID>();
            if (persistentID == null) persistentID = gameObject.AddComponent<PersistentID>();
        }
    }

    public void Interact(InventorySystem inventory)
    {
        if (inventory == null) return;

        if (currentWeapon != null)
        {
            inventory.AddItem(TakeWeapon());
        }
        else
        {
            var weaponInHand = inventory.GetCurrentItem() as VishnuWeaponItemData;
            if (weaponInHand != null)
            {
                TryPlaceWeapon(weaponInHand);
                inventory.ConsumeCurrentItem();
            }
        }
    }

    public VishnuWeaponItemData TryPlaceWeapon(VishnuWeaponItemData weapon)
    {
        if (weapon == null) return null;

        VishnuWeaponItemData previousWeapon = TakeWeapon();

        // Instantiate logic extracted to shared function
        SpawnModel(weapon);

        if (weapon.weaponType == requiredWeapon && puzzleManager != null)
        {
            puzzleManager.NotifyWeaponPlaced();
        }

        Debug.Log($"{weapon.itemName} placed in socket ({requiredWeapon}).");
        return previousWeapon;
    }

    public VishnuWeaponItemData TakeWeapon()
    {
        if (!isOccupied) return null;

        if (currentWeapon.weaponType == requiredWeapon && puzzleManager != null)
        {
            puzzleManager.NotifyWeaponRemoved();
        }

        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        VishnuWeaponItemData weaponToReturn = currentWeapon;
        currentWeapon = null;
        currentWeaponModel = null;

        return weaponToReturn;
    }

    // --- HELPER FUNCTION FOR SPAWNING ---
    private void SpawnModel(VishnuWeaponItemData weapon)
    {
        currentWeapon = weapon;
        currentWeaponModel = Instantiate(weapon.model, transform);
        
        // --- UPDATED FIX ---
        // We now IGNORE itemData.spawnRotation and itemData.spawnScale 
        // because those are for the Inventory Camera view only.
        
        // 1. Position: Center of the socket
        currentWeaponModel.transform.localPosition = Vector3.zero;

        // 2. Rotation: Align perfectly with the socket (Identity)
        currentWeaponModel.transform.localRotation = Quaternion.identity;

        // 3. Scale: Use the Prefab's original scale
        currentWeaponModel.transform.localScale = weapon.model.transform.localScale;
        // -------------------

        // Cleanup components so it doesn't act as a pickup
        if (currentWeaponModel.TryGetComponent<ItemPickup>(out var pickup)) Destroy(pickup);
        if (currentWeaponModel.TryGetComponent<Collider>(out var col)) Destroy(col);
        if (currentWeaponModel.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);
        
        currentWeaponModel.SetActive(true); 
    }

    // -------------------- ISaveable --------------------
    public string GetUniqueID()
    {
        return persistentID != null ? persistentID.id : gameObject.GetInstanceID().ToString();
    }

    public SaveObjectState CaptureState()
    {
        return new SaveObjectState
        {
            id = GetUniqueID(),
            type = "WeaponSocket",
            socketWeaponID = currentWeapon != null ? currentWeapon.uniqueID : null,
            socketWeaponName = currentWeapon != null ? (!string.IsNullOrEmpty(currentWeapon.itemName) ? currentWeapon.itemName : currentWeapon.name) : null
        };
    }

    public void RestoreState(SaveObjectState state)
    {
        if (state == null || (state.type != null && state.type != "WeaponSocket")) return;

        if (!string.IsNullOrEmpty(state.socketWeaponID))
        {
            if (currentWeapon != null && currentWeapon.uniqueID != state.socketWeaponID)
            {
                if (currentWeaponModel != null) Destroy(currentWeaponModel);
                currentWeapon = null;
                currentWeaponModel = null;
            }

            if (currentWeapon != null && currentWeapon.uniqueID == state.socketWeaponID)
            {
                if (currentWeaponModel == null) SpawnModel(currentWeapon);
            }
            else if (currentWeapon == null)
            {
                var vishnu = ResolveWeapon(state.socketWeaponID, state.socketWeaponName);
                if (vishnu != null)
                {
                    PlaceWeaponModelOnly(vishnu);
                }
                else if (fallbackWeapon != null)
                {
                    PlaceWeaponModelOnly(fallbackWeapon);
                }
            }
        }
        else
        {
            if (currentWeaponModel != null) Destroy(currentWeaponModel);
            currentWeapon = null;
            currentWeaponModel = null;
        }
    }

    private void PlaceWeaponModelOnly(VishnuWeaponItemData weapon)
    {
        if (weapon == null || weapon.model == null) return;
        SpawnModel(weapon);
    }

    private VishnuWeaponItemData ResolveWeapon(string uniqueId, string name)
    {
        ItemData item = null;

        // Try ItemRegistry by unique ID
        if (!string.IsNullOrEmpty(uniqueId))
        {
            if (ItemRegistry.Instance != null)
            {
                item = ItemRegistry.Instance.FindByUniqueID(uniqueId);
                if (item != null) Debug.Log($"[WeaponSocket] Resolved by ItemRegistry ID '{uniqueId}' => {item.name}");
            }

            // Try InventorySystem's ItemDatabase by ID
            if (item == null && InventorySystem.Instance != null && InventorySystem.Instance.itemDatabase != null)
            {
                item = InventorySystem.Instance.itemDatabase.GetItemByID(uniqueId);
                if (item != null) Debug.Log($"[WeaponSocket] Resolved by ItemDatabase ID '{uniqueId}' => {item.name}");
            }

            // Try Resources-backed ItemRegistryData (ScriptableObject)
            if (item == null)
            {
                ItemRegistryData reg = Resources.Load<ItemRegistryData>("ItemRegistry");
                if (reg == null) reg = Resources.Load<ItemRegistryData>("Items/ItemRegistry");
                if (reg == null) reg = Resources.Load<ItemRegistryData>("items/ItemRegistry");
                if (reg != null && reg.items != null)
                {
                    for (int r = 0; r < reg.items.Length; r++)
                    {
                        var it = reg.items[r];
                        if (it != null && it.uniqueID == uniqueId) { item = it; break; }
                    }
                    if (item != null) Debug.Log($"[WeaponSocket] Resolved by Resources ItemRegistryData ID '{uniqueId}' => {item.name}");
                }
            }

            // Last resort: scan Resources for any ItemData matching ID
            if (item == null)
            {
                ItemData[] poolsA = Resources.LoadAll<ItemData>("Items");
                if (item == null && poolsA != null)
                {
                    foreach (var it in poolsA) { if (it != null && it.uniqueID == uniqueId) { item = it; break; } }
                }

                ItemData[] poolsB = (item == null) ? Resources.LoadAll<ItemData>("items") : null;
                if (item == null && poolsB != null)
                {
                    foreach (var it in poolsB) { if (it != null && it.uniqueID == uniqueId) { item = it; break; } }
                }

                if (item == null)
                {
                    ItemData[] poolsAny = Resources.LoadAll<ItemData>("");
                    foreach (var it in poolsAny) { if (it != null && it.uniqueID == uniqueId) { item = it; break; } }
                }
                if (item != null) Debug.Log($"[WeaponSocket] Resolved by Resources scan ID '{uniqueId}' => {item.name}");
            }
        }

        // Fallback by name
        if (item == null && !string.IsNullOrEmpty(name))
        {
            if (ItemRegistry.Instance != null)
            {
                item = ItemRegistry.Instance.FindByName(name);
                if (item != null) Debug.Log($"[WeaponSocket] Resolved by ItemRegistry Name '{name}' => {item.name}");
            }

            if (item == null && InventorySystem.Instance != null && InventorySystem.Instance.itemDatabase != null)
            {
                var db = InventorySystem.Instance.itemDatabase;
                if (db.allItems != null)
                {
                    for (int i = 0; i < db.allItems.Length; i++)
                    {
                        var it = db.allItems[i];
                        if (it == null) continue;
                        if (it.name == name || it.itemName == name)
                        {
                            item = it;
                            break;
                        }
                    }
                }
                if (item != null) Debug.Log($"[WeaponSocket] Resolved by ItemDatabase Name '{name}' => {item.name}");
            }

            // Try name via Resources registry
            if (item == null)
            {
                ItemRegistryData reg = Resources.Load<ItemRegistryData>("ItemRegistry");
                if (reg == null) reg = Resources.Load<ItemRegistryData>("Items/ItemRegistry");
                if (reg == null) reg = Resources.Load<ItemRegistryData>("items/ItemRegistry");
                if (reg != null && reg.items != null)
                {
                    for (int r = 0; r < reg.items.Length; r++)
                    {
                        var it = reg.items[r];
                        if (it != null && (it.name == name || it.itemName == name)) { item = it; break; }
                    }
                    if (item != null) Debug.Log($"[WeaponSocket] Resolved by Resources ItemRegistryData Name '{name}' => {item.name}");
                }
            }

            // Scan Resources by name as a final fallback
            if (item == null)
            {
                ItemData[] poolsAny = Resources.LoadAll<ItemData>("");
                foreach (var it in poolsAny)
                {
                    if (it == null) continue;
                    if (it.name == name || it.itemName == name) { item = it; break; }
                }
                if (item != null) Debug.Log($"[WeaponSocket] Resolved by Resources scan Name '{name}' => {item.name}");
            }
        }

        var vishnu = item as VishnuWeaponItemData;
        if (vishnu != null) return vishnu;

        // Final fallback: try by weapon type from available assets
        vishnu = FindByWeaponType(requiredWeapon);
        return vishnu;
    }

    private VishnuWeaponItemData FindByWeaponType(VishnuWeaponType type)
    {
        // Search ItemRegistry first
        if (ItemRegistry.Instance != null && ItemRegistry.Instance.items != null)
        {
            foreach (var it in ItemRegistry.Instance.items)
            {
                var vw = it as VishnuWeaponItemData;
                if (vw != null && vw.weaponType == type)
                    return vw;
            }
        }

        // Then search ItemDatabase
        if (InventorySystem.Instance != null && InventorySystem.Instance.itemDatabase != null)
        {
            var db = InventorySystem.Instance.itemDatabase;
            if (db.allItems != null)
            {
                foreach (var it in db.allItems)
                {
                    var vw = it as VishnuWeaponItemData;
                    if (vw != null && vw.weaponType == type)
                        return vw;
                }
            }
        }

        // Lastly, use explicit fallback field if assigned
        if (fallbackWeapon != null && fallbackWeapon.weaponType == type)
            return fallbackWeapon;

        return null;
    }
}
