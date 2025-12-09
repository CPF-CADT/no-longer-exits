using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    [Header("Setup")]
    public Transform handPosition;
    public Image[] slotImages;

    [Header("Settings")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    private ItemData[] slots = new ItemData[5];
    private GameObject currentHandModel;
    private int selectedSlot = 0;

    // NEW: allow holding nothing
    private bool holdingNothing = false;
    public KeyCode unequipKey = KeyCode.Q;  // Recommended for UX

    void Start()
    {
        UpdateUI();
        SelectSlot(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // NEW: Unequip / empty hands
        if (Input.GetKeyDown(unequipKey))
        {
            ToggleEmptyHands();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    void ToggleEmptyHands()
    {
        holdingNothing = !holdingNothing;

        if (currentHandModel != null)
            Destroy(currentHandModel);

        // If now holding nothing, stop here
        if (holdingNothing) return;

        // If not holding nothing, restore item in current slot
        SpawnCurrentSlotModel();
    }

    void TryPickup()
    {
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
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UpdateUI();

                if (i == selectedSlot && !holdingNothing)
                    SelectSlot(selectedSlot);

                return;
            }
        }
        Debug.Log("Inventory Full!");
    }

    void SelectSlot(int index)
    {
        selectedSlot = index;

        // Cancel "empty hands" when selecting a real slot
        holdingNothing = false;

        // Update UI
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
            currentHandModel.transform.localRotation =
                Quaternion.Euler(slots[selectedSlot].spawnRotation);
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
