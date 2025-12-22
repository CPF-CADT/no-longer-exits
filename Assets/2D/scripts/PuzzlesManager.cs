using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PuzzlesManager : MonoBehaviour
{
    [Header("Puzzle References")]
    public Transform topSlotsParent;
    public Transform bottomPiecesParent;
    public Transform dragLayer;

    private List<ImagePieceSlotUI> pieces = new List<ImagePieceSlotUI>();

    // ================= PUZZLE REWARD =================

    [Header("Puzzle Reward Spawn")]
    public Transform[] itemParents;       // 3 parents (slots)
    public GameObject itemPrefab1;        // normal item prefab 1
    public GameObject itemPrefab2;        // normal item prefab 2
    public GameObject storyItemPrefab;    // story item prefab (needs Sprite)
    public Sprite storyImage;             // Sprite to assign to story item

    [Header("Item Spawn Scale")]
    public Vector3[] itemScales;          // Size = 3, scale for each spawned item

    public float spawnDelay = 0.5f;

    private bool hasSpawned = false;
    private bool hasWon = false;

    // ================= UNITY =================

    void Start()
    {
        pieces.AddRange(bottomPiecesParent.GetComponentsInChildren<ImagePieceSlotUI>());

        // Assign puzzle reference to pieces
        foreach (var piece in pieces)
            piece.puzzle = this;

        TopSlotUI[] slots = topSlotsParent.GetComponentsInChildren<TopSlotUI>();
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].puzzle = this;
            slots[i].correctIndex = i;

            if (slots[i].targetImage == null)
                slots[i].targetImage = slots[i].GetComponent<UnityEngine.UI.Image>();
        }
    }

    public int GetPieceIndex(ImagePieceSlotUI piece)
    {
        return pieces.IndexOf(piece);
    }

    // ================= WIN CHECK =================

public void CheckWin()
{
    if (hasWon) return;

    TopSlotUI[] slots = topSlotsParent.GetComponentsInChildren<TopSlotUI>();
    foreach (var slot in slots)
    {
        if (!slot.isSolved)
            return;
    }

    hasWon = true;
    Debug.Log("Puzzle Solved!");

    // Spawn items if needed
    if (!hasSpawned)
        StartCoroutine(SpawnPuzzleItemsRoutine());

    // --- HIDE PUZZLE CANVAS ---
    StonePuzzle stonePuzzle = FindObjectOfType<StonePuzzle>();
    if (stonePuzzle != null)
        stonePuzzle.OnPuzzleWin();
}

    // ================= SPAWN ROUTINE =================

    IEnumerator SpawnPuzzleItemsRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnPuzzleItems();
    }

    // ================= SPAWN ITEMS (CHEST-STYLE) =================

    private void SpawnPuzzleItems()
    {
        if (hasSpawned) return;

        if (itemParents == null || itemParents.Length != 3)
        {
            Debug.LogError("[PuzzlesManager] itemParents must have size = 3");
            return;
        }

        if (itemScales == null || itemScales.Length != 3)
        {
            Debug.LogWarning("[PuzzlesManager] itemScales is not set or not size 3, using Vector3.one");
            itemScales = new Vector3[3] { Vector3.one, Vector3.one, Vector3.one };
        }

        // --- SPAWN ITEM 1 ---
        SpawnPrefab(itemPrefab1, itemParents[0], itemScales[0]);

        // --- SPAWN ITEM 2 ---
        SpawnPrefab(itemPrefab2, itemParents[1], itemScales[1]);

        // --- SPAWN STORY ITEM ---
        GameObject storyItem = SpawnPrefab(storyItemPrefab, itemParents[2], itemScales[2]);

        // Inject story image like ChestController
        if (storyItem != null && storyImage != null)
        {
            ItemReadable readable = storyItem.GetComponent<ItemReadable>();
            if (readable != null)
                readable.storyImage = storyImage;
        }

        hasSpawned = true;
    }

    // ================= HELPER =================

    private GameObject SpawnPrefab(GameObject prefab, Transform parent, Vector3 localScale)
    {
        if (prefab == null || parent == null) return null;

        GameObject obj = Instantiate(prefab, parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = localScale;

        return obj;
    }
}
