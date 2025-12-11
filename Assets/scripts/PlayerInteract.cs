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
            ShootRay();
    }

    void ShootRay()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Shoot from center of the screen
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Perform Raycast
        bool hasHit = Physics.Raycast(ray, out hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // Draw Debug Line
        if (showDebugRay)
        {
            Debug.DrawLine(ray.origin, hasHit ? hit.point : ray.origin + ray.direction * interactRange,
                hasHit ? Color.green : Color.red, 2f);
        }

        // If nothing hit, exit
        if (!hasHit)
        {
            if (showDebugRay) Debug.Log("<color=red>MISS:</color> Raycast hit nothing.");
            return;
        }

        // --- INTERACTION LOGIC ---

        // 1. Check for NPC
        NPCInteract npc = hit.collider.GetComponent<NPCInteract>();
        if (npc != null)
        {
            npc.Interact();
            if (showDebugRay) Debug.Log("Interacted with NPC: " + hit.collider.name);
            return; 
        }

        // 2. Check for Save Station (NEW ADDITION)
        SaveStation station = hit.collider.GetComponent<SaveStation>();
        if (station != null)
        {
            station.Interact(); // Calls the function to update the spawn point
            if (showDebugRay) Debug.Log("<color=cyan>SAVE:</color> Game Saved at " + hit.collider.name);
            return;
        }

        // 3. Fallback (Hit something non-interactive)
        if (showDebugRay) 
            Debug.Log("<color=yellow>BLOCKED:</color> Hit object named: '" + hit.collider.name + "' but it was not interactable.");
    }
}