using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ImagePieceSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image pieceImage;
    [HideInInspector] public PuzzlesManager puzzle;

    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private bool isConsumed = false; // Add flag

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalPosition = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isConsumed) return; // Don't drag if already used

        if (puzzle == null)
        {
            Debug.LogError("Puzzle not assigned on " + gameObject.name);
            return;
        }

        originalParent = transform.parent;
        transform.SetParent(puzzle.dragLayer);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isConsumed) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // If the piece was consumed during the drop, stop here
        if (isConsumed || !gameObject.activeInHierarchy) return;

        transform.SetParent(originalParent);
        transform.position = originalPosition; // Snap back if dropped in void
        canvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData) { }

    public void ReturnToOriginal()
    {
        transform.position = originalPosition;
        transform.SetParent(originalParent);
        canvasGroup.blocksRaycasts = true;
    }

    // Call this when dropped in the correct Top Slot
    public void ConsumePiece()
    {
        isConsumed = true;
        gameObject.SetActive(false); // Hide the piece effectively "replacing" the top one
    }
}