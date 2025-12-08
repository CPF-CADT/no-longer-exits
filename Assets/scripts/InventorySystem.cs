using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    [Header("Setup")]
    public Transform handPosition; // Drag your 'HandHolder' here
    public Image[] slotImages;     // Drag your 5 UI Images here

    [Header("Settings")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    // Internal storage
    private ItemData[] slots = new ItemData[5];
    private GameObject currentHandModel;
    private int selectedSlot = 0;

    void Start()
    {
        UpdateUI();
        SelectSlot(0); // Start with slot 1
    }

    void Update()
    {
        // 1. Number Keys Selection
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // 2. Pickup Logic (Press E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    void TryPickup()
    {
        // Raycast forward from camera
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3f))
        {
            ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                AddItem(pickup.itemData);
                pickup.Pickup();
            }
        }
    }

    void AddItem(ItemData item)
    {
        // Find first empty slot
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UpdateUI();
                // If we are holding this slot, show the item immediately
                if (i == selectedSlot) SelectSlot(selectedSlot);
                return;
            }
        }
        Debug.Log("Inventory Full!");
    }

    void SelectSlot(int index)
    {
        selectedSlot = index;

        // 1. Update UI Colors
        if (slotImages != null)
        {
            for (int i = 0; i < slotImages.Length; i++)
            {
                if (slotImages[i] != null)
                    slotImages[i].color = (i == selectedSlot) ? selectedColor : normalColor;
            }
        }

        // 2. Remove old model
        if (currentHandModel != null) Destroy(currentHandModel);

        // 3. Spawn new model
        if (slots[selectedSlot] != null && slots[selectedSlot].model != null)
        {
            // Spawn the object
            currentHandModel = Instantiate(slots[selectedSlot].model, handPosition);
            
            // --- APPLY THE CUSTOM DATA FROM ITEMDATA ---
            // We use the values you typed into the Item file
            currentHandModel.transform.localPosition = slots[selectedSlot].spawnPosition;
            currentHandModel.transform.localRotation = Quaternion.Euler(slots[selectedSlot].spawnRotation);
        }
    }

    void UpdateUI()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            if (slots[i] != null)
            {
                slotImages[i].sprite = slots[i].icon;
                slotImages[i].enabled = true;
            }
            else
            {
                slotImages[i].enabled = false; // Hide empty slots
            }
        }
    }
}