using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    private void Awake() { Instance = this; }

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

    private void Start()
    {
        slots = new ItemData[totalSlots];

        // Initialize UI
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] != null) uiSlots[i].Initialize(this, i);
        }

        if (inventoryWindow != null) inventoryWindow.SetActive(false);
        UpdateUI();
        SelectSlot(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(inventoryKey)) ToggleInventoryScreen();

        // Hotbar keys
        for (int i = 0; i < hotbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectSlot(i);
        }

        if (!isInventoryOpen)
        {
            if (Input.GetKeyDown(unequipKey)) ToggleEmptyHands();
            if (Input.GetKeyDown(interactKey)) HandleInteraction();
            if (Input.GetKeyDown(readKey)) CheckHandForSpecialAbility();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // --- ITEM MANAGEMENT ---
    public void AddItem(ItemData item)
    {
        if (item == null) return;

        // Prevent duplicates for non-stackable items
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == item)
            {
                Debug.Log("Item already in inventory!");
                return;
            }
        }

        // Fill Hotbar first
        for (int i = 0; i < hotbarSize; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UpdateUI();
                if (i == selectedSlot && !holdingNothing) SelectSlot(selectedSlot);
                return;
            }
        }

        // Then Backpack
        for (int i = hotbarSize; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UpdateUI();
                return;
            }
        }

        Debug.Log("Inventory Full!");
    }

    public void SelectSlot(int index)
    {
        if (index >= hotbarSize) return;

        selectedSlot = index;
        holdingNothing = false;
        UpdateUI();

        if (currentHandModel != null) Destroy(currentHandModel);
        SpawnCurrentSlotModel();
    }

    private void SpawnCurrentSlotModel()
    {
        if (holdingNothing || slots[selectedSlot] == null || slots[selectedSlot].model == null) return;

        if (currentHandModel != null) Destroy(currentHandModel);

        currentHandModel = Instantiate(slots[selectedSlot].model, handPosition);
        currentHandModel.transform.localPosition = slots[selectedSlot].spawnPosition;
        currentHandModel.transform.localRotation = Quaternion.Euler(slots[selectedSlot].spawnRotation);
        currentHandModel.transform.localScale = slots[selectedSlot].spawnScale;

        // Remove unnecessary components
        if (currentHandModel.TryGetComponent<ItemPickup>(out var pickup)) Destroy(pickup);
        if (currentHandModel.TryGetComponent<Collider>(out var col)) Destroy(col);
        if (currentHandModel.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);

        if (currentHandModel.TryGetComponent<ItemReadable>(out var readable) && slots[selectedSlot].storyImage != null)
            readable.storyImage = slots[selectedSlot].storyImage;
    }

    public void ConsumeCurrentItem()
    {
        if (slots[selectedSlot] == null) return;

        if (slots[selectedSlot].isConsumable)
        {
            Destroy(currentHandModel);
            slots[selectedSlot] = null;
            UpdateUI();
            SpawnCurrentSlotModel();
        }
    }

    // --- UI ---
    private void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] == null) continue;
            bool isSelected = (i == selectedSlot);
            uiSlots[i].UpdateSlot(slots[i], isSelected);
        }
    }

    private void ToggleInventoryScreen()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryWindow != null) inventoryWindow.SetActive(isInventoryOpen);

        if (!isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
        if (pickup != null && pickup.TryClaim())
        {
            AddItem(pickup.itemData);
            pickup.Pickup();
        }
    }

    private void CheckHandForSpecialAbility()
    {
        if (currentHandModel == null) return;

        if (currentHandModel.TryGetComponent<ItemReadable>(out var readable) && readable.storyImage != null)
        {
            ScrollManager.Instance.OpenScroll(readable.storyImage);
            ConsumeCurrentItem();
        }
    }

    // --- SAVE / LOAD ---
    [System.Serializable]
    public class InventorySlotSave
    {
        public string itemUniqueID; // or itemID

    }

    public InventorySlotSave[] GetSaveInventory()
    {
        InventorySlotSave[] data = new InventorySlotSave[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                data[i] = new InventorySlotSave { itemUniqueID = slots[i].uniqueID };
            }
        }
        return data;
    }

    public void LoadInventoryFromSave(InventorySlotSave[] savedSlots, int savedSelectedSlot, bool holdingEmpty)
    {
        if (savedSlots == null || uiSlots == null) return;

        slots = new ItemData[totalSlots]; // initialize

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = null; // clear first

            if (i < savedSlots.Length && savedSlots[i] != null && !string.IsNullOrEmpty(savedSlots[i].itemUniqueID))
            {
                string id = savedSlots[i].itemUniqueID;
                ItemData item = null;

                // 1) Prefer ItemDatabase (explicit list of all items)
                if (itemDatabase != null)
                {
                    item = itemDatabase.GetItemByID(id);
                }

                // 2) Fallback: scene ItemRegistry singleton (if present)
                if (item == null)
                {
                    item = ItemRegistry.Instance?.FindByUniqueID(id);
                }

                // 3a) Fallback: Resources ItemRegistryData (ScriptableObject)
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
                            if (it != null && it.uniqueID == id) { item = it; break; }
                        }
                    }
                }

                // 3b) Fallback: Resources lookup (any folder, case-insensitive common paths)
                if (item == null)
                {
                    // Common folder names
                    ItemData[] poolsA = Resources.LoadAll<ItemData>("Items");
                    if (item == null && poolsA != null)
                    {
                        foreach (var it in poolsA) { if (it != null && it.uniqueID == id) { item = it; break; } }
                    }

                    ItemData[] poolsB = (item == null) ? Resources.LoadAll<ItemData>("items") : null;
                    if (item == null && poolsB != null)
                    {
                        foreach (var it in poolsB) { if (it != null && it.uniqueID == id) { item = it; break; } }
                    }

                    // Last resort: scan entire Resources
                    if (item == null)
                    {
                        ItemData[] poolsAny = Resources.LoadAll<ItemData>("");
                        foreach (var it in poolsAny) { if (it != null && it.uniqueID == id) { item = it; break; } }
                    }
                }

                if (item == null)
                {
                    Debug.LogWarning($"Inventory load: Could not resolve item with ID '{id}'. Ensure it exists in ItemDatabase or ItemRegistry.");
                }

                slots[i] = item;
            }
        }

        selectedSlot = Mathf.Clamp(savedSelectedSlot, 0, hotbarSize - 1);
        holdingNothing = holdingEmpty;

        UpdateUI();

        if (!holdingNothing)
            SpawnCurrentSlotModel();
    }


    public int GetSelectedSlotIndex() => selectedSlot;
    public bool GetHoldingNothing() => holdingNothing;
    public ItemData GetCurrentItem() => holdingNothing ? null : slots[selectedSlot];

    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Length || indexB < 0 || indexB >= slots.Length) return;

        ItemData temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        // Update hand model if selected slot changed
        if (indexA == selectedSlot || indexB == selectedSlot)
        {
            if (currentHandModel != null) Destroy(currentHandModel);
            if (!holdingNothing) SpawnCurrentSlotModel();
        }

        UpdateUI();
    }

}
