using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    // Left click to use item (ghost banish / special)
    public KeyCode useItemKey = KeyCode.Mouse0;
    public float ghostBanishRange = 5f;

    [Header("Debug")]
    public bool showDebugRay = true;

    private InventorySystem inventory;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();
        if (inventory == null)
            inventory = GetComponentInParent<InventorySystem>();
        if (inventory == null)
            Debug.LogError("CRITICAL ERROR: PlayerInteract cannot find the InventorySystem!");
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
            ShootRay(false);

        if (Input.GetKeyDown(useItemKey))
            ShootRay(true);
    }

    void ShootRay(bool isUsingItem)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float range = isUsingItem ? ghostBanishRange : interactRange;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (showDebugRay)
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * range, Color.red, 2f);
            return;
        }

        if (showDebugRay)
            Debug.DrawLine(ray.origin, hit.point, Color.green, 2f);

        if (isUsingItem)
        {
            // Ghost banishment
            NPCRoaming ghost = hit.collider.GetComponent<NPCRoaming>();
            if (ghost != null)
            {
                ItemData itemInHand = inventory.GetCurrentItem();
                if (itemInHand != null)
                {
                    bool banished = ghost.AttemptBanish(itemInHand);
                    if (banished && itemInHand.isConsumable)
                        inventory.ConsumeCurrentItem();
                }
            }
            return;
        }

        // Normal interaction
        NPCInteract npc = hit.collider.GetComponent<NPCInteract>();
        if (npc != null) { npc.Interact(); return; }

        SaveStation station = hit.collider.GetComponent<SaveStation>();
        if (station != null) { station.Interact(); return; }

        WeaponSocket socket = hit.collider.GetComponent<WeaponSocket>();
        ItemData currentItem = inventory.GetCurrentItem();

        // If interacting with a weapon socket
        if (socket != null)
        {
            // If socket is occupied, pick up weapon back
            if (socket.isOccupied)
            {
                VishnuWeaponItemData weapon = socket.TakeWeapon();
                if (weapon != null)
                    inventory.AddItem(weapon);
                return;
            }

            // Only place a weapon if holding one
            if (currentItem is VishnuWeaponItemData weaponInHand)
            {
                socket.TryPlaceWeapon(weaponInHand);
                inventory.ConsumeCurrentItem();
            }

            return;
        }

        // Item pickup: only allow if not holding anything
        if (currentItem == null)
        {
            ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                inventory.AddItem(pickup.itemData);
                pickup.Pickup();
            }
        }
    }
}
