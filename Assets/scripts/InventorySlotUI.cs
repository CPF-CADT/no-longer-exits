using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("References")]
    public Image iconImage;
    public Image borderImage;

    [Header("Debug Info")]
    public int slotIndex;
    public ItemData currentItem;

    private InventorySystem inventory;
    private Transform originalIconParent; 
    private CanvasGroup iconCanvasGroup;

    public void Initialize(InventorySystem system, int index)
    {
        inventory = system;
        slotIndex = index;
        
        // Add a CanvasGroup to the Icon if it doesn't exist (Helper for transparency/raycast)
        if (iconImage != null)
        {
            iconCanvasGroup = iconImage.GetComponent<CanvasGroup>();
            if (iconCanvasGroup == null) iconCanvasGroup = iconImage.gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void UpdateSlot(ItemData item, bool isSelected)
    {
        currentItem = item;

        // VISUALS
        if (currentItem != null)
        {
            iconImage.sprite = currentItem.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }

        // BORDER (Always visible logic)
        if (borderImage != null)
        {
            borderImage.enabled = true; 
            borderImage.color = isSelected ? inventory.selectedColor : inventory.normalColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        inventory.SelectSlot(slotIndex);
    }

    // --- DRAG AND DROP FIX ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        // 1. Remember the slot this icon belongs to
        originalIconParent = iconImage.transform.parent;

        // 2. Move to DragLayer so it's on top of everything
        iconImage.transform.SetParent(inventory.dragCanvasTransform);

        // 3. Disable Raycast so we can detect the slot UNDER the mouse
        if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;
        iconImage.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // FIX: ALWAYS return the icon to its original parent
        // Even if we swapped items, UpdateUI() will run instantly after this 
        // and fix the sprite. If we dropped on "nothing", this snaps it back home.
        if (originalIconParent != null)
        {
            iconImage.transform.SetParent(originalIconParent);
            iconImage.transform.localPosition = Vector3.zero;
        }

        // Re-enable Raycast
        if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // This runs on the DESTINATION slot
        InventorySlotUI incomingSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        
        if (incomingSlot != null)
        {
            // Swap Data
            inventory.SwapItems(incomingSlot.slotIndex, slotIndex);
        }
    }
}