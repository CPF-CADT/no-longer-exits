using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    [Header("Setup")]
    public Transform handPosition;
    public Image[] slotImages;

    [Header("Settings")]
    public float interactRange = 3f; 
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    // Inventory State
    private ItemData[] slots = new ItemData[5];
    private GameObject currentHandModel;
    private int selectedSlot = 0;
    private bool holdingNothing = false;

    // Keys
    public KeyCode interactKey = KeyCode.E;
    public KeyCode unequipKey = KeyCode.Q;
    public KeyCode readKey = KeyCode.F; 

    void Start()
    {
        UpdateUI();
        SelectSlot(0);
    }

    void Update()
    {
        // 1. Slot Selection
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // 2. Unequip
        if (Input.GetKeyDown(unequipKey)) ToggleEmptyHands();

        // 3. Interaction (Doors, Items, Chests)
        if (Input.GetKeyDown(interactKey)) HandleInteraction();

        // 4. Special Ability (Reading)
        if (Input.GetKeyDown(readKey)) CheckHandForSpecialAbility();
    }

    // --- INTERACTION LOGIC (Includes Key Checks) ---
    void HandleInteraction()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // CHECK 1: Items
            ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                AddItem(pickup.itemData);
                pickup.Pickup();
                return; 
            }

            // CHECK 2: Doors (UPDATED)
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
            {
                // We pass the item in our current slot to the door!
                // The door checks if it matches the "Required Key"
                door.ToggleDoor(slots[selectedSlot]); 
                return;
            }

            // CHECK 3: Chests
            ChestController chest = hit.collider.GetComponentInParent<ChestController>();
            if (chest != null)
            {
                // We pass the item in our current slot to the chest
                chest.OpenChest(slots[selectedSlot]);
                return;
            }
        }
    }

    // --- SPECIAL ABILITY (Simple Read - No Transform) ---
    void CheckHandForSpecialAbility()
    {
        if (currentHandModel == null) return;

        // Find the script that holds the image
        ItemReadable readable = currentHandModel.GetComponent<ItemReadable>();

        if (readable != null)
        {
            // Open UI Directly (No 3D destruction)
            if (ScrollManager.Instance != null && readable.storyImage != null)
            {
                ScrollManager.Instance.OpenScroll(readable.storyImage);
            }
            return;
        }

        Debug.Log("This item cannot be read.");
    }

    // --- INVENTORY MANAGEMENT ---
    void ToggleEmptyHands()
    {
        holdingNothing = !holdingNothing;
        if (currentHandModel != null) Destroy(currentHandModel);
        if (holdingNothing) return;
        SpawnCurrentSlotModel();
    }

    void AddItem(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UpdateUI();
                if (i == selectedSlot && !holdingNothing) SelectSlot(selectedSlot);
                return;
            }
        }
        Debug.Log("Inventory Full!");
    }

    void SelectSlot(int index)
    {
        selectedSlot = index;
        holdingNothing = false;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] != null)
                slotImages[i].color = (i == selectedSlot) ? selectedColor : normalColor;
        }

        if (currentHandModel != null) Destroy(currentHandModel);
        SpawnCurrentSlotModel();
    }

    void SpawnCurrentSlotModel()
    {
        if (holdingNothing) return;

        if (slots[selectedSlot] != null && slots[selectedSlot].model != null)
        {
            currentHandModel = Instantiate(slots[selectedSlot].model, handPosition);
            currentHandModel.transform.localPosition = slots[selectedSlot].spawnPosition;
            currentHandModel.transform.localRotation = Quaternion.Euler(slots[selectedSlot].spawnRotation);

            // Cleanup Physics
            ItemPickup pickupScript = currentHandModel.GetComponent<ItemPickup>();
            if (pickupScript != null) Destroy(pickupScript);
            Collider col = currentHandModel.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Rigidbody rb = currentHandModel.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            // Transfer Sprite from Inventory Data to Hand Script
            ItemReadable readable = currentHandModel.GetComponent<ItemReadable>();
            if (readable != null && slots[selectedSlot].storyImage != null)
            {
                readable.storyImage = slots[selectedSlot].storyImage;
            }
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
                slotImages[i].enabled = false;
            }
        }
    }
}