using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    // Frame index of the last successful interaction handled by this component
    public static int lastInteractFrame = -1;

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

        ItemData currentItem = inventory.GetCurrentItem();

        // --- Using item (ghost banish)
        if (isUsingItem)
        {
            NPCRoaming ghost = hit.collider.GetComponent<NPCRoaming>();
            if (ghost != null && currentItem != null)
            {
                bool banished = ghost.AttemptBanish(currentItem);
                if (banished && currentItem.isConsumable)
                    inventory.ConsumeCurrentItem();

                lastInteractFrame = Time.frameCount;
            }
            return;
        }

        // --- Normal interactions
        NPCInteract npc = hit.collider.GetComponent<NPCInteract>();
        if (npc != null) { npc.Interact(); lastInteractFrame = Time.frameCount; return; }

        SaveStation station = hit.collider.GetComponent<SaveStation>();
        if (station != null) { station.Interact(); lastInteractFrame = Time.frameCount; return; }

        DoorController door = hit.collider.GetComponentInParent<DoorController>();
        if (door != null)
        {
            door.ToggleDoor(currentItem);
            lastInteractFrame = Time.frameCount;
            return;
        }

        WeaponSocket socket = hit.collider.GetComponent<WeaponSocket>();
        if (socket != null)
        {
            // --- Handle socket weapon interaction properly
            if (socket.isOccupied || currentItem is VishnuWeaponItemData)
            {
                // If socket is full, take the weapon back
                if (socket.isOccupied)
                {
                    VishnuWeaponItemData weapon = socket.TakeWeapon();
                    if (weapon != null)
                        inventory.AddItem(weapon);

                    lastInteractFrame = Time.frameCount;
                    return;
                }

                // If socket is empty, try to place weapon from hand
                if (currentItem is VishnuWeaponItemData)
                {
                    // Keep a reference to the weapon before removing from inventory
                    var weaponToPlace = inventory.RemoveCurrentItem() as VishnuWeaponItemData;
                    if (weaponToPlace != null)
                    {
                        socket.TryPlaceWeapon(weaponToPlace);
                    }

                    lastInteractFrame = Time.frameCount;
                    return;
                }
            }

            // Fallback: call generic Interact for sockets without special weapon handling
            socket.Interact(inventory);
            lastInteractFrame = Time.frameCount;
            return;
        }

        ChestController chest = hit.collider.GetComponentInParent<ChestController>();
        if (chest != null)
        {
            chest.OpenChest(currentItem);
            lastInteractFrame = Time.frameCount;
            return;
        }

        // --- Item pickup: only allow if not holding anything
        if (currentItem == null)
        {
            ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
            if (pickup != null && pickup.TryClaim())
            {
                inventory.AddItem(pickup.itemData);
                pickup.Pickup();
                lastInteractFrame = Time.frameCount;
            }
        }
    }

}