using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    [Header("Setup")]
    public Transform handPosition;
    public Image[] slotImages;

    [Header("Settings")]
    public float interactRange = 3f; // Distance for pickup and doors
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

    void Start()
    {
        UpdateUI();
        SelectSlot(0);
    }

    void Update()
    {
        // 1. Handle Slot Selection
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // 2. Handle Unequip
        if (Input.GetKeyDown(unequipKey))
        {
            ToggleEmptyHands();
        }

        // 3. Handle Interaction (Doors AND Items)
        if (Input.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }
    }

    // THIS IS THE NEW MERGED RAYCAST FUNCTION
    void HandleInteraction()
    {
        // Shoot ray from the center of the screen (Main Camera)
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // CHECK 1: Did we hit an Item?
            ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                AddItem(pickup.itemData);
                pickup.Pickup();
                return; // Stop here (don't try to open a door if we just picked up an item)
            }

            // CHECK 2: Did we hit a Door? (Using GetComponentInParent to fix hierarchy issues)
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
            {
                door.ToggleDoor();
                return;
            }
        }
    }

    // --- Standard Inventory Logic Below ---

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

        // UI Updates
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
            // 1. Spawn the model
            currentHandModel = Instantiate(slots[selectedSlot].model, handPosition);
            currentHandModel.transform.localPosition = slots[selectedSlot].spawnPosition;
            currentHandModel.transform.localRotation = Quaternion.Euler(slots[selectedSlot].spawnRotation);

            // ================================================================
            // 2. FIX: REMOVE PHYSICS AND SCRIPTS SO YOU DON'T CLICK YOURSELF
            // ================================================================
            
            // Remove the Pickup Script so Raycast doesn't see it as an item
            ItemPickup pickupScript = currentHandModel.GetComponent<ItemPickup>();
            if (pickupScript != null) Destroy(pickupScript);

            // Remove/Disable Collider so the Ray passes through it
            Collider col = currentHandModel.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Remove Rigidbody so it doesn't have gravity/physics
            Rigidbody rb = currentHandModel.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
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