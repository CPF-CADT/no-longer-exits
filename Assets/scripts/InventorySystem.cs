using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("UI References")]
    public Transform handPosition;
    public Transform dragCanvasTransform; // Assign "DragLayer" here (see Part 3)
    public GameObject inventoryWindow;    // Assign "InventoryWindow" panel here
    public InventorySlotUI[] uiSlots;     // Drag ALL 20 Slot objects here

    [Header("Settings")]
    public int totalSlots = 20;
    public int hotbarSize = 6;            // Slots 0-5 are always visible
    public float interactRange = 3f;
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    // DATA
    private ItemData[] slots;
    private GameObject currentHandModel;
    private int selectedSlot = 0;
    private bool holdingNothing = false;
    private bool isInventoryOpen = false;

    // INPUTS
    public KeyCode interactKey = KeyCode.E;
    public KeyCode unequipKey = KeyCode.Q;
    public KeyCode readKey = KeyCode.F;
    public KeyCode inventoryKey = KeyCode.Tab;

    void Start()
    {
        slots = new ItemData[totalSlots];

        // Initialize UI Scripts
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] != null) uiSlots[i].Initialize(this, i);
        }

        if (inventoryWindow != null) inventoryWindow.SetActive(false);
        UpdateUI();
        SelectSlot(0);
    }

    void Update()
    {
        // Tab to toggle inventory
        if (Input.GetKeyDown(inventoryKey)) ToggleInventoryScreen();

        // Hotbar keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);

        if (!isInventoryOpen)
        {
            if (Input.GetKeyDown(unequipKey)) ToggleEmptyHands();
            if (Input.GetKeyDown(interactKey)) HandleInteraction();
            if (Input.GetKeyDown(readKey)) CheckHandForSpecialAbility();
        }
        else
        {
            // Unlock mouse when inventory is open
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SwapItems(int indexA, int indexB)
    {
        ItemData temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        if (indexA == selectedSlot || indexB == selectedSlot) SelectSlot(selectedSlot);
        UpdateUI();
    }

    void ToggleInventoryScreen()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryWindow != null) inventoryWindow.SetActive(isInventoryOpen);

        if (!isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleInteraction()
    {
        if (PlayerInteract.lastInteractFrame == Time.frameCount) return;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange)) return;

        ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            if (pickup.TryClaim())
            {
                AddItem(pickup.itemData);
                pickup.Pickup();
            }
            return;
        }

        DoorController door = hit.collider.GetComponentInParent<DoorController>();
        if (door != null) { door.ToggleDoor(slots[selectedSlot]); return; }

        ChestController chest = hit.collider.GetComponentInParent<ChestController>();
        if (chest != null) { chest.OpenChest(slots[selectedSlot]); return; }
    }

    void ToggleEmptyHands()
    {
        holdingNothing = !holdingNothing;
        if (currentHandModel != null) Destroy(currentHandModel);
        if (!holdingNothing) SpawnCurrentSlotModel();
    }

    public void AddItem(ItemData item)
    {
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
        if (index >= hotbarSize) return; // Can only equip hotbar items
        selectedSlot = index;
        holdingNothing = false;
        UpdateUI();
        if (currentHandModel != null) Destroy(currentHandModel);
        SpawnCurrentSlotModel();
    }

    void SpawnCurrentSlotModel()
    {
        if (holdingNothing || slots[selectedSlot] == null || slots[selectedSlot].model == null) return;
        if (currentHandModel != null) Destroy(currentHandModel);

        currentHandModel = Instantiate(slots[selectedSlot].model, handPosition);
        currentHandModel.transform.localPosition = slots[selectedSlot].spawnPosition;
        currentHandModel.transform.localRotation = Quaternion.Euler(slots[selectedSlot].spawnRotation);
        currentHandModel.transform.localScale = slots[selectedSlot].spawnScale;

        if (currentHandModel.TryGetComponent<ItemPickup>(out var pickup)) Destroy(pickup);
        if (currentHandModel.TryGetComponent<Collider>(out var col)) Destroy(col);
        if (currentHandModel.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);

        if (currentHandModel.TryGetComponent<ItemReadable>(out var readable) && slots[selectedSlot].storyImage != null)
            readable.storyImage = slots[selectedSlot].storyImage;
    }

    void CheckHandForSpecialAbility()
    {
        if (currentHandModel == null) return;
        if (currentHandModel.TryGetComponent<ItemReadable>(out var readable) && readable.storyImage != null && ScrollManager.Instance != null)
        {
            ScrollManager.Instance.OpenScroll(readable.storyImage);
            ConsumeCurrentItem();
        }
    }

    void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] == null) continue;
            bool isSelected = (i == selectedSlot);
            uiSlots[i].UpdateSlot(slots[i], isSelected);
        }
    }

    public void ConsumeCurrentItem()
    {
        if (slots[selectedSlot] == null) return;
        if (slots[selectedSlot].isConsumable || slots[selectedSlot] is VishnuWeaponItemData)
        {
            Destroy(currentHandModel);
            slots[selectedSlot] = null;
            UpdateUI();
            SpawnCurrentSlotModel();
        }
    }

    // --- SAVE / LOAD (PRESERVED) ---

    public string[] GetSaveInventory()

    {
        Debug.Log("SAVING INVENTORY. Array Size is: " + slots.Length); // <--- ADD THIS
        string[] data = new string[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            data[i] = (slots[i] != null) ? slots[i].name : null;
        }
        return data;
    }

    public void LoadInventoryFromNames(string[] names, int selectedIndex, bool holdingEmpty)
    {
        if (names == null) return;

        // Loop through slots.Length (20). If the save file only has 5 items,
        // the condition (i < names.Length) protects us from crashing.
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = null;
            if (i < names.Length && !string.IsNullOrEmpty(names[i]))
            {
                ItemRegistryData registryData = Resources.Load<ItemRegistryData>("ItemRegistry");
                if (registryData != null)
                {
                    slots[i] = registryData.FindByName(names[i]);
                    if (slots[i] != null) continue;
                }

                if (ItemRegistry.Instance != null)
                {
                    slots[i] = ItemRegistry.Instance.FindByName(names[i]);
                    if (slots[i] != null) continue;
                }

                ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
                for (int j = 0; j < allItems.Length; j++)
                {
                    if (allItems[j].name == names[i] || allItems[j].itemName == names[i])
                    {
                        slots[i] = allItems[j];
                        break;
                    }
                }
            }
        }

        UpdateUI();
        selectedSlot = Mathf.Clamp(selectedIndex, 0, hotbarSize - 1); // Clamp to hotbar
        holdingNothing = holdingEmpty;

        if (currentHandModel != null) Destroy(currentHandModel);
        if (!holdingNothing) SpawnCurrentSlotModel();
    }

    public int GetSelectedSlotIndex() => selectedSlot;

    public bool GetHoldingNothing() => holdingNothing;

    public ItemData GetCurrentItem()
    {
        if (holdingNothing || slots[selectedSlot] == null) return null;
        return slots[selectedSlot];
    }
}