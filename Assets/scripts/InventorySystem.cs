using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    // --- FIX 1: Initialize array in Awake to prevent Save Wipe ---
    private void Awake()
    {
        // Singleton Setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // CRITICAL: Create the empty array here so it exists before loading data
        slots = new ItemData[totalSlots];
    }

    [Header("UI References")]
    public Transform handPosition;
    public Transform dragCanvasTransform;
    public GameObject inventoryWindow;
    public InventorySlotUI[] uiSlots;

    [Header("Settings")]
    public int totalSlots = 20;
    public int hotbarSize = 6;
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;
    [Header("Duplication Settings")]
    public List<ItemData> duplicableItems;

    // DATA
    private ItemData[] slots;
    private GameObject currentHandModel;
    private int selectedSlot = 0;
    private bool holdingNothing = false;
    private bool isInventoryOpen = false;

    [Header("Inputs")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode unequipKey = KeyCode.Q;
    public KeyCode readKey = KeyCode.F;
    public KeyCode inventoryKey = KeyCode.Tab;

    [Header("Item Database")]
    public ItemDatabase itemDatabase;
    [Header("Item Scroll Reference")]
    public ItemData scroll;

    private void Start()
    {
        // NOTE: We do NOT do 'slots = new ItemData...' here.
        // It is done in Awake() to protect your Save Data.

        // Initialize UI Logic
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] != null) uiSlots[i].Initialize(this, i);
        }

        if (inventoryWindow != null)
        {
            inventoryWindow.SetActive(false);
            isInventoryOpen = false;
        }

        // Only update UI if we didn't just load from save
        UpdateUI();

        // If we loaded data, we might need to spawn the model
        if (slots[selectedSlot] != null && !holdingNothing)
        {
            SpawnCurrentSlotModel();
        }
        else
        {
            // If nothing loaded, select first slot
            SelectSlot(0);
        }
    }

    private void Update()
    {
        // Prevent inventory toggle if puzzle UI is open
        if (Input.GetKeyDown(inventoryKey) && !IsPuzzleUIOpen())
        {
            ToggleInventoryScreen();
        }

        if (!isInventoryOpen)
        {
            // Hotbar selection (number keys + mouse scroll)
            HandleHotbarInput();

            // Other inputs when inventory is closed
            if (Input.GetKeyDown(unequipKey)) ToggleEmptyHands();
            if (Input.GetKeyDown(interactKey)) HandleInteraction();
            if (Input.GetKeyDown(readKey)) CheckHandForSpecialAbility();

            // Lock cursor when inventory is closed and puzzle UI not open
            if (!IsPuzzleUIOpen())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else
        {
            // Unlock cursor when inventory is open
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    // Called from Update to check if a puzzle is currently active
    private bool IsPuzzleUIOpen()
    {
        StonePuzzle puzzle = FindObjectOfType<StonePuzzle>();
        return puzzle != null && puzzle.puzzleCanvas != null && puzzle.puzzleCanvas.activeSelf;
    }


    private void ToggleInventoryScreen()
    {
        // Prevent opening inventory if a puzzle is active
        if (IsPuzzleUIOpen()) return;

        isInventoryOpen = !isInventoryOpen;
        if (inventoryWindow != null) inventoryWindow.SetActive(isInventoryOpen);

        if (!isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }



    // --- ITEM MANAGEMENT ---
    public bool AddItem(ItemData item)
    {
        if (item == null) return false;

        if (slots == null)
            slots = new ItemData[totalSlots];

        // Scroll special case
        if (scroll != null && item.UniqueID == scroll.UniqueID)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].UniqueID == item.UniqueID)
                {
                    ScrollManager.Instance?.EnqueueStoryIfNotPresent(item.storyImage, false);
                    return true; // already owned → treat as success
                }
            }
        }

        // Prevent duplicates unless allowed
        bool allowDuplicate = duplicableItems != null && duplicableItems.Contains(item);
        if (!allowDuplicate)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].UniqueID == item.UniqueID)
                    return false;
            }
        }

        // Find ANY empty slot (hotbar OR backpack)
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UpdateUI();

                if (i < hotbarSize && i == selectedSlot && !holdingNothing)
                    SpawnCurrentSlotModel();

                return true; // ✅ SUCCESS
            }
        }

        Debug.LogWarning("Inventory FULL — item NOT added");
        return false; // ❌ FAILURE
    }


    public void SelectSlot(int index)
    {
        if (slots == null) return;
        if (index < 0 || index >= slots.Length) return;

        selectedSlot = index;
        holdingNothing = false;
        UpdateUI();

        if (currentHandModel != null)
            Destroy(currentHandModel);

        if (index < hotbarSize)
            SpawnCurrentSlotModel();
    }

    private float scrollAccumulator = 0f;
    private float scrollThreshold = 0.1f; // Amount of scroll needed to change slot

    private void HandleHotbarInput()
    {
        if (IsPuzzleUIOpen()) return;

        // --- NUMBER KEYS (1-6) ---
        for (int i = 0; i < hotbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }

        // --- MOUSE SCROLL ---
        // --- MOUSE SCROLL ---
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            scrollAccumulator += scrollInput;

            if (Mathf.Abs(scrollAccumulator) >= scrollThreshold)
            {
                // Invert direction
                int direction = scrollAccumulator > 0 ? -1 : 1; // flipped
                int newSlot = (selectedSlot + direction + hotbarSize) % hotbarSize; // wrap around
                SelectSlot(newSlot);
                scrollAccumulator = 0f; // reset after switching
            }
        }
    }


    private void SpawnCurrentSlotModel()
    {
        if (slots == null) return;

        // Check if we have an item and it has a model
        if (holdingNothing || slots[selectedSlot] == null || slots[selectedSlot].model == null) return;

        // Clean up old model
        if (currentHandModel != null) Destroy(currentHandModel);

        // Get the data
        ItemData data = slots[selectedSlot];

        // Instantiate: 
        // passing 'false' as the 3rd argument ensures we ignore the prefab's world position
        // and spawn it relative to the handPosition immediately.
        currentHandModel = Instantiate(data.model, handPosition, false);

        // --- APPLY TRANSFORMS FROM SCRIPTABLE OBJECT ---
        currentHandModel.transform.localPosition = data.spawnPosition;
        currentHandModel.transform.localRotation = Quaternion.Euler(data.spawnRotation);
        currentHandModel.transform.localScale = data.spawnScale;

        // Remove unnecessary components for the "In Hand" version
        if (currentHandModel.TryGetComponent<ItemPickup>(out var pickup)) Destroy(pickup);
        if (currentHandModel.TryGetComponent<Collider>(out var col)) Destroy(col);
        if (currentHandModel.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);

        // Apply Story Image if readable
        if (currentHandModel.TryGetComponent<ItemReadable>(out var readable) && data.storyImage != null)
            readable.storyImage = data.storyImage;
    }

    // --- REMOVE ITEM ---
    public ItemData RemoveCurrentItem()
    {
        if (slots == null || slots[selectedSlot] == null) return null;

        // Keep a reference to the item before removing
        ItemData removedItem = slots[selectedSlot];

        // Destroy the hand model if it exists
        if (currentHandModel != null)
            Destroy(currentHandModel);

        // Clear the slot
        slots[selectedSlot] = null;

        // Update UI
        UpdateUI();

        // Return the removed item
        return removedItem;
    }


    public void ConsumeCurrentItem()
    {
        if (slots == null || slots[selectedSlot] == null) return;

        if (slots[selectedSlot].isConsumable)
        {
            RemoveCurrentItem();
        }
    }

    // --- UI ---
    private void UpdateUI()
    {
        if (uiSlots == null || slots == null) return;

        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] == null) continue;
            bool isSelected = (i == selectedSlot);
            uiSlots[i].UpdateSlot(slots[i], isSelected);
        }
    }

    private void ToggleEmptyHands()
    {
        holdingNothing = !holdingNothing;
        if (currentHandModel != null) Destroy(currentHandModel);
        if (!holdingNothing) SpawnCurrentSlotModel();
    }

    private void HandleInteraction()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, 3f)) return;

        ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
        if (pickup == null) return;

        ItemData item = pickup.itemData;
        if (item == null) return;

        // Try to add FIRST
        bool added = AddItem(item);

        // Inventory full → DO NOTHING (item stays in world)
        if (!added)
        {
            Debug.Log("Inventory full. Item NOT picked up.");
            return;
        }

        // Only destroy pickup AFTER successful add
        pickup.Pickup();

        if (item.storyImage != null)
            ScrollManager.Instance?.EnqueueStoryIfNotPresent(item.storyImage, false);
    }



    private void CheckHandForSpecialAbility()
    {
        if (currentHandModel == null) return;

        if (currentHandModel.TryGetComponent<ItemReadable>(out var readable))
        {
            if (readable.storyImage == null)
            {
                Debug.LogWarning("ItemReadable has no storyImage assigned.");
                return;
            }

            if (ScrollManager.Instance == null)
            {
                Debug.LogWarning("ScrollManager.Instance is not set in the scene.");
                return;
            }

            // Enqueue the story safely
            ScrollManager.Instance.EnqueueStoryIfNotPresent(readable.storyImage, autoOpenIfFirst: false);
            ScrollManager.Instance.OpenIfAny();

            // Consume the item if it's consumable
            ConsumeCurrentItem();
        }
    }

    // --- FIX 2: SAFE GET CURRENT ITEM ---
    // This prevents the NullReferenceException if other scripts call it too early
    public ItemData GetCurrentItem()
    {
        // Safety Check 1: If the array doesn't exist yet, return null
        if (slots == null) return null;

        // Safety Check 2: If we are holding nothing, return null
        if (holdingNothing) return null;

        // Safety Check 3: Make sure the selected slot is valid
        if (selectedSlot < 0 || selectedSlot >= slots.Length) return null;

        // Return the item
        return slots[selectedSlot];
    }

    public int GetSelectedSlotIndex() => selectedSlot;
    public bool GetHoldingNothing() => holdingNothing;

    public void SwapItems(int indexA, int indexB)
    {
        if (slots == null) return;
        if (indexA < 0 || indexA >= slots.Length || indexB < 0 || indexB >= slots.Length) return;

        ItemData temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        if (indexA == selectedSlot || indexB == selectedSlot)
        {
            if (currentHandModel != null) Destroy(currentHandModel);
            if (!holdingNothing) SpawnCurrentSlotModel();
        }

        UpdateUI();
    }

    public int GetItemCount(ItemData itemToCheck)
    {
        if (itemToCheck == null || slots == null) return 0;

        int count = 0;
        foreach (var slot in slots)
        {
            if (slot != null && slot.UniqueID == itemToCheck.UniqueID)
                count++;
        }
        return count;
    }


    // --- SAVE / LOAD ---
    [System.Serializable]
    public class InventorySlotSave
    {
        public string itemUniqueID;
    }

    public InventorySlotSave[] GetSaveInventory()
    {
        if (slots == null) return new InventorySlotSave[0];

        InventorySlotSave[] data = new InventorySlotSave[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                data[i] = new InventorySlotSave { itemUniqueID = slots[i].UniqueID };
            }
        }
        return data;
    }

    // --- FIX 3: ROBUST LOAD WITH DEBUGGING ---
    public void LoadInventoryFromSave(InventorySlotSave[] savedSlots, int savedSelectedSlot, bool holdingEmpty)
    {
        Debug.Log("--- STARTING LOAD PROCESS ---");

        if (itemDatabase == null)
        {
            Debug.LogError("CRITICAL ERROR: ItemDatabase is NOT assigned in the Inspector!");
            return;
        }

        // Initialize Database
        itemDatabase.Init();
        Debug.Log($"Database Status: Loaded {itemDatabase.allItems.Length} items.");

        // Ensure slots are initialized
        if (slots == null || slots.Length != totalSlots)
            slots = new ItemData[totalSlots];

        // Clear slots to prevent ghost items
        System.Array.Clear(slots, 0, slots.Length);

        if (savedSlots == null)
        {
            Debug.LogWarning("Save file contained NO inventory data.");
            return;
        }

        Debug.Log($"Save file contains {savedSlots.Length} items to load.");

        for (int i = 0; i < slots.Length; i++)
        {
            // Skip if out of bounds or empty in save
            if (i >= savedSlots.Length || savedSlots[i] == null) continue;

            string id = savedSlots[i].itemUniqueID;

            if (string.IsNullOrEmpty(id)) continue;

            Debug.Log($"Slot [{i}]: Trying to load Item ID: {id}");

            // 1. Try Database
            ItemData item = itemDatabase.GetItemByID(id);

            // 2. Try Fallback (Resource Load) if Database fails
            if (item == null)
            {
                // Try simple resources load as fallback
                ItemData[] poolsAny = Resources.LoadAll<ItemData>("");
                foreach (var it in poolsAny) { if (it != null && it.UniqueID == id) { item = it; break; } }
            }

            if (item != null)
            {
                Debug.Log($"<color=green>SUCCESS:</color> Found item '{item.itemName}' for ID: {id}");
                slots[i] = item;
            }
            else
            {
                Debug.LogError($"<color=red>FAILED:</color> Item ID {id} was NOT found in ItemDatabase OR Resources! Did you add it to the MainDatabase list?");
            }
        }

        selectedSlot = Mathf.Clamp(savedSelectedSlot, 0, slots.Length - 1);
        holdingNothing = holdingEmpty;

        UpdateUI();

        if (!holdingNothing)
            SpawnCurrentSlotModel();

        Debug.Log("--- LOAD PROCESS FINISHED ---");
    }

}