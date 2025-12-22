// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.EventSystems;

// [RequireComponent(typeof(CanvasGroup))] // Logic needs to be on the SLOT, not the image
// public class PuzzleSourceSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
// {
//     [Header("UI Structure")]
//     public Image iconImage;   // DRAG YOUR ICON HERE
//     public Image borderImage; // DRAG YOUR BORDER HERE

//     [Header("Settings")]
//     public CanvasGroup iconCanvasGroup; // Attach CanvasGroup on the ICON object (optional, auto-found)

//     private Transform originalIconParent; // Where the icon lives normally
    
//     private void Start()
//     {
//         // Auto-setup CanvasGroup if missing on the Icon
//         if (iconImage != null && iconCanvasGroup == null)
//         {
//             iconCanvasGroup = iconImage.GetComponent<CanvasGroup>();
//             if (iconCanvasGroup == null) iconCanvasGroup = iconImage.gameObject.AddComponent<CanvasGroup>();
//         }
//     }

//     public void OnBeginDrag(PointerEventData eventData)
//     {
//         if (iconImage == null) return;

//         // 1. Save where the icon belongs (This Slot)
//         originalIconParent = iconImage.transform.parent;

//         // 2. "Dereference" / Detach: Move Icon to the global Drag Layer
//         // So it floats above everything else
//         if (PuzzlesManager.Instance != null)
//         {
//             iconImage.transform.SetParent(PuzzlesManager.Instance.dragLayer);
//         }

//         // 3. Allow Raycasts to pass through the icon so we hit the Drop Slot
//         if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = false;
//     }

//     public void OnDrag(PointerEventData eventData)
//     {
//         if (iconImage == null) return;
//         // Move the Icon, not this Slot
//         iconImage.transform.position = eventData.position;
//     }

//     public void OnEndDrag(PointerEventData eventData)
//     {
//         if (iconImage == null) return;

//         // 4. Return Icon to Home (This Slot)
//         // Even if we dropped it, we snap back first. 
//         // If the drop was successful, the Target Slot will consume/hide this object anyway.
//         iconImage.transform.SetParent(originalIconParent);
        
//         // 5. Snap to center (0,0,0)
//         iconImage.transform.localPosition = Vector3.zero;

//         // 6. Block Raycasts again
//         if (iconCanvasGroup != null) iconCanvasGroup.blocksRaycasts = true;
//     }

//     public void PieceSolved()
//     {
//         // Hide the whole slot or just the icon when solved
//         gameObject.SetActive(false); 
//     }
// }