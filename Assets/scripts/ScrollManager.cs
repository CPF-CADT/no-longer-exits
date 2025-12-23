using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Needed for UI

public class ScrollManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiPanel;   // Drag your "ScrollCanvas" or Panel here
    public Image displayImage;   // Drag the "StoryDisplay" Image here
    public Button prevButton;    // Optional: UI Button for previous
    public Button nextButton;    // Optional: UI Button for next

    [Header("Optional Keys (avoid arrows)")]
    public KeyCode nextKey = KeyCode.Period;  // '>' on many keyboards
    public KeyCode prevKey = KeyCode.Comma;   // '<' on many keyboards

    [Header("Lookup (Optional)")]
    [Tooltip("Optional database to resolve ItemData by uniqueID for story restore")] public ItemDatabase itemDatabase;

    // Singleton (This allows other scripts to find this one easily)
    public static ScrollManager Instance;

    // Internal queue of collected stories (track both ID and Sprite for persistence)
    private struct StoryEntry { public string id; public Sprite sprite; }
    private readonly List<StoryEntry> stories = new List<StoryEntry>();
    private int currentIndex = -1; // -1 means nothing selected

    void Awake()
    {
        Instance = this;
        if (uiPanel != null) uiPanel.SetActive(false); // Hide at start

        // Wire optional buttons if provided
        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(PrevStoryOnClick);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStoryOnClick);
        }
    }

    void Update()
    {
        // Press F or Escape to close
        if (uiPanel != null && uiPanel.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)))
        {
            CloseScroll();
        }

        // While UI is open, allow optional keys for navigation
        if (uiPanel != null && uiPanel.activeSelf)
        {
            if (Input.GetKeyDown(nextKey)) NextStory();
            if (Input.GetKeyDown(prevKey)) PrevStory();
        }
    }

    // Backward-compatible API: now enqueues and opens the newly added story
    public void OpenScroll(Sprite img)
    {
        if (img == null) return;
        bool wasEmpty = stories.Count == 0;
        EnqueueStoryIfNotPresent(img);

        // If adding the first story or UI is closed, open on the newest
        if (wasEmpty || (uiPanel != null && !uiPanel.activeSelf))
        {
            currentIndex = stories.Count - 1;
            ShowCurrent();
            OpenPanel();
        }
    }

    // Enqueue a story sprite; optionally auto-open if it's the first one
    public void EnqueueStory(Sprite img, bool autoOpenIfFirst = true)
    {
        if (img == null) return;
        bool wasEmpty = stories.Count == 0;
        stories.Add(new StoryEntry { id = null, sprite = img });

        // If UI is open, keep current index; just refresh nav state
        if (uiPanel != null && uiPanel.activeSelf)
        {
            RefreshNavigation();
            return;
        }

        if (autoOpenIfFirst && wasEmpty)
        {
            currentIndex = 0;
            ShowCurrent();
            OpenPanel();
        }
    }

    // Enqueue only if not already present (reference equality check)
    public void EnqueueStoryIfNotPresent(Sprite img, bool autoOpenIfFirst = false)
    {
        if (img == null) return;
        bool exists = false;
        for (int i = 0; i < stories.Count; i++)
        {
            if (stories[i].sprite == img) { exists = true; break; }
        }
        if (!exists)
        {
            EnqueueStory(img, autoOpenIfFirst);
        }
        else
        {
            // If stories exist and panel is closed, optionally open
            if (autoOpenIfFirst && (uiPanel != null && !uiPanel.activeSelf) && stories.Count > 0)
            {
                if (currentIndex < 0) currentIndex = 0;
                ShowCurrent();
                OpenPanel();
            }
        }
    }

    // Open the panel if there are any stories collected
    public void OpenIfAny()
    {
        if (stories.Count == 0) return;
        if (currentIndex < 0 || currentIndex >= stories.Count) currentIndex = 0;
        ShowCurrent();
        OpenPanel();
    }

    public void CloseScroll()
    {
        if (uiPanel == null) return;

        uiPanel.SetActive(false); // Hide UI

        // Optional: Lock cursor again for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void NextStoryOnClick() => NextStory();
    public void PrevStoryOnClick() => PrevStory();

    public void NextStory()
    {
        if (stories.Count == 0) return;
        if (currentIndex < stories.Count - 1)
        {
            currentIndex++;
            ShowCurrent();
        }
        RefreshNavigation();
    }

    public void PrevStory()
    {
        if (stories.Count == 0) return;
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowCurrent();
        }
        RefreshNavigation();
    }

    private void ShowCurrent()
    {
        if (displayImage == null) return;
        if (currentIndex < 0 || currentIndex >= stories.Count) return;
        displayImage.sprite = stories[currentIndex].sprite;
        displayImage.preserveAspect = true;
        RefreshNavigation();
    }

    private void OpenPanel()
    {
        if (uiPanel == null) return;
        uiPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshNavigation();
    }

    private void RefreshNavigation()
    {
        bool hasStories = stories.Count > 0;
        bool canPrev = hasStories && currentIndex > 0;
        bool canNext = hasStories && currentIndex < stories.Count - 1;

        if (prevButton != null) prevButton.interactable = canPrev;
        if (nextButton != null) nextButton.interactable = canNext;
    }

    // -------------------- Item-based enqueue for persistence --------------------
    public void EnqueueItemStory(ItemData item, bool autoOpenIfFirst = true)
    {
        if (item == null || item.storyImage == null) return;
        string id = item.UniqueID;
        bool wasEmpty = stories.Count == 0;

        // Deduplicate by ID if available; else by sprite reference
        if (!string.IsNullOrEmpty(id))
        {
            for (int i = 0; i < stories.Count; i++) if (stories[i].id == id) { RefreshNavigation(); TryAutoOpenIfFirst(autoOpenIfFirst, wasEmpty); return; }
            stories.Add(new StoryEntry { id = id, sprite = item.storyImage });
        }
        else
        {
            for (int i = 0; i < stories.Count; i++) if (stories[i].sprite == item.storyImage) { RefreshNavigation(); TryAutoOpenIfFirst(autoOpenIfFirst, wasEmpty); return; }
            stories.Add(new StoryEntry { id = null, sprite = item.storyImage });
        }

        if (uiPanel != null && uiPanel.activeSelf)
        {
            RefreshNavigation();
            return;
        }

        TryAutoOpenIfFirst(autoOpenIfFirst, wasEmpty);
    }

    public void EnqueueItemStoryIfNotPresent(ItemData item, bool autoOpenIfFirst = false)
    {
        EnqueueItemStory(item, autoOpenIfFirst);
    }

    private void TryAutoOpenIfFirst(bool autoOpenIfFirst, bool wasEmpty)
    {
        if (autoOpenIfFirst && wasEmpty)
        {
            currentIndex = 0;
            ShowCurrent();
            OpenPanel();
        }
    }

    // -------------------- Save/Load API --------------------
    public List<string> GetSavedStoryIDs()
    {
        var ids = new List<string>();
        for (int i = 0; i < stories.Count; i++)
        {
            if (!string.IsNullOrEmpty(stories[i].id)) ids.Add(stories[i].id);
        }
        return ids;
    }

    public int GetCurrentStoryIndex() => currentIndex;

    public void LoadStoriesFromIDs(List<string> ids, int selectedIndex = 0)
    {
        stories.Clear();
        if (ids == null || ids.Count == 0)
        {
            currentIndex = -1;
            RefreshNavigation();
            return;
        }

        for (int i = 0; i < ids.Count; i++)
        {
            var item = ResolveItemByID(ids[i]);
            if (item != null && item.storyImage != null)
            {
                stories.Add(new StoryEntry { id = ids[i], sprite = item.storyImage });
            }
        }

        currentIndex = Mathf.Clamp(selectedIndex, 0, stories.Count - 1);

        // If panel is currently open, make sure it shows current
        if (uiPanel != null && uiPanel.activeSelf)
        {
            ShowCurrent();
        }
        else
        {
            RefreshNavigation();
        }
    }

    private ItemData ResolveItemByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        // 1) Prefer ItemDatabase
        if (itemDatabase != null)
        {
            var it = itemDatabase.GetItemByID(id);
            if (it != null) return it;
        }

        // 2) ItemRegistry singleton
        var regInst = ItemRegistry.Instance;
        if (regInst != null)
        {
            var it = regInst.FindByUniqueID(id);
            if (it != null) return it;
        }

        // 3a) Resources ItemRegistryData
        ItemRegistryData reg = Resources.Load<ItemRegistryData>("ItemRegistry");
        if (reg == null) reg = Resources.Load<ItemRegistryData>("Items/ItemRegistry");
        if (reg == null) reg = Resources.Load<ItemRegistryData>("items/ItemRegistry");
        if (reg != null && reg.items != null)
        {
            for (int r = 0; r < reg.items.Length; r++)
            {
                var it = reg.items[r];
                if (it != null && it.UniqueID == id) return it;
            }
        }

        // 3b) Fallback scan all ItemData in Resources
        ItemData[] poolsAny = Resources.LoadAll<ItemData>("");
        for (int i = 0; i < poolsAny.Length; i++) { var it = poolsAny[i]; if (it != null && it.UniqueID == id) return it; }
        return null;
    }
}