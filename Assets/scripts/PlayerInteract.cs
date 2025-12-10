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

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        bool hasHit = Physics.Raycast(ray, out hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (showDebugRay)
        {
            Debug.DrawLine(ray.origin, hasHit ? hit.point : ray.origin + ray.direction * interactRange,
                hasHit ? Color.green : Color.red, 2f);
        }

        if (!hasHit) 
        {
            if (showDebugRay) Debug.Log("<color=red>MISS:</color> Raycast hit nothing.");
            return;
        }

        // First check Door
        DoorController door = hit.collider.GetComponentInParent<DoorController>();
        if (door != null)
        {
            door.ToggleDoor();
            return;
        }

        // Then check Chest
        ChestController chest = hit.collider.GetComponentInParent<ChestController>();
        if (chest != null)
        {
            chest.OpenChest();
            return;
        }

        Debug.Log("<color=yellow>BLOCKED:</color> Hit object named: '" + hit.collider.name + "'");
    }
}
