using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    // NEW: Click to use item on Ghost
    public KeyCode useItemKey = KeyCode.Mouse0;
    public float ghostBanishRange = 5f;

    [Header("Debug")]
    public bool showDebugRay = true;

    private InventorySystem inventory;

    void Start()
    {
        // Try to find the inventory on this object
        inventory = GetComponent<InventorySystem>();

        // SAFETY CHECK: If not found, try finding it on the parent or anywhere in the player
        if (inventory == null)
            inventory = GetComponentInParent<InventorySystem>();

        if (inventory == null)
            Debug.LogError("CRITICAL ERROR: PlayerInteract cannot find the 'InventorySystem' script on the Player!");
    }

    void Update()
    {
        // 1. Normal Interaction (E)
        if (Input.GetKeyDown(interactKey))
            ShootRay(false);

        // 2. Use Item (Left Click)
        if (Input.GetKeyDown(useItemKey))
            ShootRay(true);
    }

    void ShootRay(bool isUsingItem)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float range = isUsingItem ? ghostBanishRange : interactRange;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        bool hasHit = Physics.Raycast(ray, out hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (showDebugRay)
        {
            Debug.DrawLine(ray.origin, hasHit ? hit.point : ray.origin + ray.direction * range,
                hasHit ? Color.green : Color.red, 2f);
        }

        if (!hasHit) return;

        // --- GHOST BANISHMENT (Left Click) ---
        if (isUsingItem)
        {
            // SAFETY CHECK: If inventory is missing, stop here to prevent crash
            if (inventory == null)
            {
                Debug.LogWarning("Cannot banish ghost: Inventory System is missing!");
                return;
            }

            NPCRoaming ghost = hit.collider.GetComponent<NPCRoaming>();

            if (ghost != null)
            {
                ItemData itemInHand = inventory.GetCurrentItem();

                if (itemInHand != null)
                {
                    bool banished = ghost.AttemptBanish(itemInHand); // return true if banish worked

                    // Only consume the item if it's marked as consumable and banish succeeded
                    if (banished && itemInHand.isConsumable)
                    {
                        inventory.ConsumeCurrentItem();
                    }
                }
            }

            return;
        }

        // --- NORMAL INTERACTION (E) ---

        // 1. NPC Dialogue
        NPCInteract npc = hit.collider.GetComponent<NPCInteract>();
        if (npc != null)
        {
            npc.Interact();
            return;
        }

        // 2. Save Station
        SaveStation station = hit.collider.GetComponent<SaveStation>();
        if (station != null)
        {
            station.Interact();
            return;
        }
    }
}