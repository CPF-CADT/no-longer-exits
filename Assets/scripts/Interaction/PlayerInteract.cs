using TMPro;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public static int lastInteractFrame = -1;

    [Header("Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode useItemKey = KeyCode.Mouse0;
    public float ghostBanishRange = 5f;

    [Header("Debug")]
    public bool showDebugRay = true;

    [Header("References")]
    public Camera playerCamera;
    public TextMeshProUGUI hintText;

    [Header("Performance")]
    public float hintRaycastInterval = 0.1f; // seconds
    private float lastHintTime = 0f;

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
        if (playerCamera == null) return;

        // Update hint text only at intervals to save performance
        if (Time.time - lastHintTime >= hintRaycastInterval)
        {
            UpdateHintText();
            lastHintTime = Time.time;
        }

        // Handle interactions
        if (Input.GetKeyDown(interactKey))
            ShootRay(false);
        if (Input.GetKeyDown(useItemKey))
            ShootRay(true);
    }

    void UpdateHintText()
    {
        hintText.text = "";

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        // Get InteractionHint from hit or parent
        InteractionHint hint = hit.collider.GetComponent<InteractionHint>() 
                             ?? hit.collider.GetComponentInParent<InteractionHint>();

        if (hint != null)
        {
            // HideBox special case
            HideBox hideBox = hit.collider.GetComponentInParent<HideBox>();
            if (hideBox != null)
                hint.useAlternate = hideBox.IsPlayerHidden;

            // DoorController special case
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
                hint.useAlternate = door.isOpen;

            hintText.text = hint.GetHintText();
        }
        else
        {
            // No hint to show
            hintText.text = "";
        }
    }

    void ShootRay(bool isUsingItem)
    {
        Camera cam = playerCamera;
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
        if (hit.collider.GetComponent<NPCInteract>() is NPCInteract npc) 
        { 
            npc.Interact(); 
            lastInteractFrame = Time.frameCount; 
            return; 
        }

        if (hit.collider.GetComponent<SaveStation>() is SaveStation station) 
        { 
            station.Interact(); 
            lastInteractFrame = Time.frameCount; 
            return; 
        }

        if (hit.collider.GetComponentInParent<DoorController>() is DoorController doorCtrl) 
        { 
            doorCtrl.ToggleDoor(currentItem); 
            lastInteractFrame = Time.frameCount; 
            return; 
        }

        if (hit.collider.GetComponentInParent<HideBox>() is HideBox hideBox) 
        { 
            hideBox.ToggleHide(); 
            lastInteractFrame = Time.frameCount; 
            return; 
        }

        if (hit.collider.GetComponent<WeaponSocket>() is WeaponSocket socket) 
        { 
            socket.Interact(inventory); 
            lastInteractFrame = Time.frameCount; 
            return; 
        }

        if (hit.collider.GetComponentInParent<ChestController>() is ChestController chest) 
        { 
            chest.OpenChest(currentItem); 
            lastInteractFrame = Time.frameCount; 
            return; 
        }

        if (currentItem == null && hit.collider.GetComponent<ItemPickup>() is ItemPickup pickup && pickup.TryClaim())
        {
            inventory.AddItem(pickup.itemData);
            pickup.Pickup();
            lastInteractFrame = Time.frameCount;
        }
    }
}
