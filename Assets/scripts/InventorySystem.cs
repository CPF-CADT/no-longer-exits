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
    public KeyCode readKey = KeyCode.F; // Key to read the item in hand

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

        // 3. Handle Interaction (Doors AND Items on floor)
        if (Input.GetKeyDown(interactKey))
        {
            HandleInteraction();
        }

        // 4. Handle Special Ability (Reading item in hand)
        if (Input.GetKeyDown(readKey))
        {
            CheckHandForSpecialAbility();
        }
    }

    // --- INTERACTION LOGIC ---
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
                return; // Stop here
            }

            // CHECK 2: Did we hit a Door?
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
            {
                door.ToggleDoor();
                return;
            }
        }
    }

    // --- SPECIAL ABILITY (F Key) ---
    void CheckHandForSpecialAbility()
    {
        // 1. Safety Check: Are we holding anything?
        if (currentHandModel == null) return;

        // 2. Try to find the "ItemReadable" script on the object in our hand
        ItemReadable readable = currentHandModel.GetComponent<ItemReadable>();

        // 3. If found, Open the Scroll!
        if (readable != null)
        {
            ScrollManager.Instance.OpenScroll(readable.storyImage);
            return;
        }

        // You can add other abilities here later (e.g., Flashlight)
        Debug.Log("This item has no special 'F' function.");
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
            // 1. Spawn the model in hand
            currentHandModel = Instantiate(slots[selectedSlot].model, handPosition);
            currentHandModel.transform.localPosition = slots[selectedSlot].spawnPosition;
            currentHandModel.transform.localRotation = Quaternion.Euler(slots[selectedSlot].spawnRotation);

            // 2. Remove Physics/Pickup scripts (so we don't click ourselves)
            ItemPickup pickupScript = currentHandModel.GetComponent<ItemPickup>();
            if (pickupScript != null) Destroy(pickupScript);
            Collider col = currentHandModel.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Rigidbody rb = currentHandModel.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            // =========================================================
            // 3. TRANSFER THE SPRITE TO THE HAND SCRIPT (The Fix)
            // =========================================================
            ItemReadable readable = currentHandModel.GetComponent<ItemReadable>();
            
            // If this item has a Readable script AND we have an image in our inventory data...
            if (readable != null && slots[selectedSlot].storyImage != null)
            {
                // Pass the image from the Inventory Data -> Hand Script
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