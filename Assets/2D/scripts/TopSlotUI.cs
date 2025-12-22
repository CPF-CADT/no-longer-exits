using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TopSlotUI : MonoBehaviour, IDropHandler
{
    public int correctIndex; 
    public PuzzlesManager puzzle;

    // The image component inside this slot that we will change
    public Image targetImage; 

    // NEW: Drag the specific image you want to appear here in the Inspector!
    public Sprite solvedSprite; 
    
    [HideInInspector] public bool isSolved = false;

    void Start()
    {
        // Auto-find Target Image if not assigned
        if (targetImage == null)
        {
            Transform pieceChild = transform.Find("PieceImage");
            if (pieceChild != null) targetImage = pieceChild.GetComponent<Image>();
        }

        // Hide the image initially
        if(targetImage != null)
        {
            Color c = targetImage.color;
            c.a = 0f; // Invisible until solved
            targetImage.color = c;
            targetImage.gameObject.SetActive(true);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isSolved) return;

        ImagePieceSlotUI piece = eventData.pointerDrag.GetComponent<ImagePieceSlotUI>();
        if (piece != null)
        {
            int pieceIndex = puzzle.GetPieceIndex(piece);

            if (pieceIndex == correctIndex)
            {
                // --- CORRECT MATCH ---
                
                if (targetImage != null)
                {
                    // CHANGE 1: Use the specific 'solvedSprite' you set in the Inspector
                    if (solvedSprite != null)
                    {
                        targetImage.sprite = solvedSprite;
                    }
                    else
                    {
                        // Fallback: If you forgot to set solvedSprite, use the dragged piece's image
                        targetImage.sprite = piece.pieceImage.sprite;
                    }

                    // CHANGE 2: Make sure it's visible
                    Color c = targetImage.color;
                    c.a = 1f;
                    targetImage.color = c;
                }

                isSolved = true;
                Debug.Log("is solved");
                piece.ConsumePiece(); 
                puzzle.CheckWin();
            }
            else
            {
                piece.ReturnToOriginal();
            }
        }
    }
}