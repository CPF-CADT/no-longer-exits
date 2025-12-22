using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PuzzlesManager : MonoBehaviour
{
    public Transform topSlotsParent;
    public Transform bottomPiecesParent;
    public Transform dragLayer;
    // public TextMeshProUGUI winText; // Suggest using TextMeshProUGUI for Canvas

    private List<ImagePieceSlotUI> pieces = new List<ImagePieceSlotUI>();

    void Start()
    {
        pieces.AddRange(bottomPiecesParent.GetComponentsInChildren<ImagePieceSlotUI>());
        // if(winText) winText.gameObject.SetActive(false);

        // Assign puzzle reference to pieces
        foreach (var piece in pieces)
            piece.puzzle = this;

        TopSlotUI[] slots = topSlotsParent.GetComponentsInChildren<TopSlotUI>();
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].puzzle = this;
            slots[i].correctIndex = i;
            // Ensure the slot knows its own image component
            if (slots[i].targetImage == null) 
                slots[i].targetImage = slots[i].GetComponent<UnityEngine.UI.Image>();
        }
    }

    public int GetPieceIndex(ImagePieceSlotUI piece)
    {
        return pieces.IndexOf(piece);
    }

    public void CheckWin()
    {
        TopSlotUI[] slots = topSlotsParent.GetComponentsInChildren<TopSlotUI>();
        foreach (var slot in slots)
        {
            if (!slot.isSolved)
                return; // someone is not solved yet
        }

        Debug.Log("You Win!");
        // if(winText) {
        //     winText.gameObject.SetActive(true);
        //     winText.text = "You Win!";
        // }
    }
}