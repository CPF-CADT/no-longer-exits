using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Debug")]
    public bool showDebugRay = true;

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            ShootRay();
        }
    }

    void ShootRay()
    {
        // 1. Create a precise Ray from the center of the screen
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // 2. Use Raycast (Laser) instead of SphereCast (Ball)
        // This ensures the ray goes exactly where the crosshair points,
        // allowing you to pick items inside the chest without hitting the lid.
        bool hasHit = Physics.Raycast(ray, out hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // --- VISUAL DEBUGGING ---
        if (showDebugRay)
        {
            if (hasHit)
            {
                // Hit something: Draw GREEN line to the object
                Debug.DrawLine(ray.origin, hit.point, Color.green, 2f);
            }
            else
            {
                // Hit nothing: Draw RED line to max distance
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * interactRange, Color.red, 2f);
            }
        }

        // --- INTERACTION LOGIC ---
        if (hasHit)
        {
            // 1. Check for Door
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            
            // 2. Check for Chest
            ChestController chest = hit.collider.GetComponentInParent<ChestController>();

            if (door != null)
            {
                door.ToggleDoor();
                Debug.Log("<color=green>SUCCESS:</color> Opened Door via " + hit.collider.name);
            }
            else if (chest != null)
            {
                chest.OpenChest();
                Debug.Log("<color=green>SUCCESS:</color> Opened Chest via " + hit.collider.name);
            }
            else
            {
                // Hit something else (Wall, Floor, etc.)
                Debug.Log("<color=yellow>BLOCKED:</color> Hit object named: '" + hit.collider.name + "'");
            }
        }
        else
        {
            if (showDebugRay) Debug.Log("<color=red>MISS:</color> Raycast hit nothing.");
        }
    }

    // Visualizes the Raycast in the Scene view when the game is not running
    // private void OnDrawGizmos()
    // {
    //     if (Camera.main == null) return;

    //     Gizmos.color = Color.red;
    //     Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
    //     // Draw a straight line representing the precise Raycast
    //     Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * interactRange);
    // }
}