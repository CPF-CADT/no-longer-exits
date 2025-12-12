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
        // Slot selection
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // Unequip
        if (Input.GetKeyDown(unequipKey)) ToggleEmptyHands();

        // Interaction
        if (Input.GetKeyDown(interactKey)) HandleInteraction();

        // Reading / special ability
        if (Input.GetKeyDown(readKey)) CheckHandForSpecialAbility();
    }

    void HandleInteraction()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            // Item pickup
            ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                AddItem(pickup.itemData);
                pickup.Pickup();
                return;
            }

            // Door
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
            {
                door.ToggleDoor(slots[selectedSlot]);
                return;
            }

            // Chest
            ChestController chest = hit.collider.GetComponentInParent<ChestController>();
            if (chest != null)
            {
                chest.OpenChest(slots[selectedSlot]);
                return;
            }
        }
    }

    void ToggleEmptyHands()
    {
        holdingNothing = !holdingNothing;
        if (currentHandModel != null) Destroy(currentHandModel);
        if (!holdingNothing) SpawnCurrentSlotModel();
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
        if (holdingNothing || slots[selectedSlot] == null || slots[selectedSlot].model == null)
            return;

        if (currentHandModel != null) Destroy(currentHandModel);

        // Instantiate as child of handPosition
        currentHandModel = Instantiate(slots[selectedSlot].model, handPosition);

        // Apply exact spawn values from ItemData
        currentHandModel.transform.localPosition = slots[selectedSlot].spawnPosition;
        currentHandModel.transform.localRotation = Quaternion.Euler(slots[selectedSlot].spawnRotation);
        currentHandModel.transform.localScale = slots[selectedSlot].spawnScale;

        // Cleanup physics
        var pickup = currentHandModel.GetComponent<ItemPickup>();
        if (pickup != null) Destroy(pickup);

        var col = currentHandModel.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rb = currentHandModel.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Transfer readable story image
        var readable = currentHandModel.GetComponent<ItemReadable>();
        if (readable != null && slots[selectedSlot].storyImage != null)
            readable.storyImage = slots[selectedSlot].storyImage;
    }

    void CheckHandForSpecialAbility()
    {
        if (currentHandModel == null) return;

        ItemReadable readable = currentHandModel.GetComponent<ItemReadable>();
        if (readable != null && readable.storyImage != null && ScrollManager.Instance != null)
        {
            ScrollManager.Instance.OpenScroll(readable.storyImage);
            ConsumeCurrentItem();
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

    public ItemData GetCurrentItem()
    {
        if (holdingNothing || slots[selectedSlot] == null) return null;
        return slots[selectedSlot];
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
}
