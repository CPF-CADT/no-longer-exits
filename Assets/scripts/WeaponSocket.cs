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

        var weaponInHand = inventory.GetCurrentItem() as VishnuWeaponItemData;
        if (weaponInHand == null) return;

        // Swap current weapon if socket is occupied
        VishnuWeaponItemData previousWeapon = TakeWeapon();
        if (previousWeapon != null)
            inventory.AddItem(previousWeapon);

        // Place the new weapon
        TryPlaceWeapon(weaponInHand);

        // Remove the weapon from inventory if successfully placed
        if (currentWeapon == weaponInHand)
            inventory.ConsumeCurrentItem();
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
        if (weapon == null || weapon.model == null) return;

        if (currentWeaponModel != null) Destroy(currentWeaponModel);

        currentWeapon = weapon;
        currentWeaponModel = Instantiate(weapon.model, transform);

        // Position & rotation
        currentWeaponModel.transform.localPosition = Vector3.zero;
        currentWeaponModel.transform.localRotation = weapon.model.transform.localRotation;
        currentWeaponModel.transform.localScale = weapon.model.transform.localScale;

        // Remove pickup/collider/rigidbody from all children
        foreach (var pickup in currentWeaponModel.GetComponentsInChildren<ItemPickup>())
            Destroy(pickup);
        foreach (var col in currentWeaponModel.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (var rb in currentWeaponModel.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

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
            socketWeaponID = currentWeapon != null ? currentWeapon.UniqueID : null,
            socketWeaponName = currentWeapon != null ? (!string.IsNullOrEmpty(currentWeapon.itemName) ? currentWeapon.itemName : currentWeapon.name) : null
        };
    }

    public void RestoreState(SaveObjectState state)
    {
        if (state == null || (state.type != null && state.type != "WeaponSocket")) return;

        if (!string.IsNullOrEmpty(state.socketWeaponID))
        {
            if (currentWeapon != null && currentWeapon.UniqueID != state.socketWeaponID)
            {
                if (currentWeaponModel != null) Destroy(currentWeaponModel);
                currentWeapon = null;
                currentWeaponModel = null;
            }

            if (currentWeapon != null && currentWeapon.UniqueID == state.socketWeaponID)
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
            }

            // Try InventorySystem's ItemDatabase by ID
            if (item == null && InventorySystem.Instance != null && InventorySystem.Instance.itemDatabase != null)
            {
                item = InventorySystem.Instance.itemDatabase.GetItemByID(uniqueId);
            }

            // Try Resources-backed ItemRegistryData (ScriptableObject)
            if (item == null)
            {
                ItemRegistryData reg = Resources.Load<ItemRegistryData>("ItemRegistry");
                if (reg == null) reg = Resources.Load<ItemRegistryData>("Items/ItemRegistry");
                if (reg != null && reg.items != null)
                {
                    for (int r = 0; r < reg.items.Length; r++)
                    {
                        var it = reg.items[r];
                        if (it != null && it.UniqueID == uniqueId) { item = it; break; }
                    }
                }
            }

            // Last resort: scan Resources for any ItemData matching ID
            if (item == null)
            {
                ItemData[] poolsA = Resources.LoadAll<ItemData>("Items");
                if (item == null && poolsA != null)
                {
                    foreach (var it in poolsA) { if (it != null && it.UniqueID == uniqueId) { item = it; break; } }
                }

                if (item == null)
                {
                    ItemData[] poolsAny = Resources.LoadAll<ItemData>("");
                    foreach (var it in poolsAny) { if (it != null && it.UniqueID == uniqueId) { item = it; break; } }
                }
            }
        }

        // Fallback by name
        if (item == null && !string.IsNullOrEmpty(name))
        {
            if (ItemRegistry.Instance != null)
            {
                item = ItemRegistry.Instance.FindByName(name);
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
            }

            // Try name via Resources registry
            if (item == null)
            {
                ItemRegistryData reg = Resources.Load<ItemRegistryData>("ItemRegistry");
                if (reg == null) reg = Resources.Load<ItemRegistryData>("Items/ItemRegistry");
                if (reg != null && reg.items != null)
                {
                    for (int r = 0; r < reg.items.Length; r++)
                    {
                        var it = reg.items[r];
                        if (it != null && (it.name == name || it.itemName == name)) { item = it; break; }
                    }
                }
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

        if (fallbackWeapon != null && fallbackWeapon.weaponType == type)
            return fallbackWeapon;

        return null;
    }
}