using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    
    // "Thick" ray setting: makes it easier to hit thin objects
    public float interactRadius = 0.5f; 

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
        // 1. Create a Ray from the EXACT center of the screen (0.5, 0.5)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        RaycastHit hit;

        // 2. Shoot the SphereCast
        bool hasHit = Physics.SphereCast(ray, interactRadius, out hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // --- VISUAL DEBUGGING ---
        if (showDebugRay)
        {
            if (hasHit)
            {
                // Hit something: Draw GREEN line to the object
                Debug.DrawLine(ray.origin, hit.point, Color.green, 2f);
                // Draw a sphere at the hit point to show how "thick" the detection is
                DebugExtension.DebugWireSphere(hit.point, Color.green, interactRadius, 2f);
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
            // Check for Door
            DoorController door = hit.collider.GetComponentInParent<DoorController>();

            if (door != null)
            {
                door.ToggleDoor();
                Debug.Log("<color=green>SUCCESS:</color> Opened Door via " + hit.collider.name);
            }
            else
            {
                // This tells you EXACTLY what is blocking the ray (e.g., "Wall", "Floor", "InvisibleCollider")
                Debug.Log("<color=yellow>BLOCKED:</color> Hit object named: '" + hit.collider.name + "'");
            }
        }
        else
        {
            Debug.Log("<color=red>MISS:</color> Raycast hit nothing.");
        }
    }

    // Helper to draw the sphere in the Scene view
    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;

        // Visualize the "Thickness" of your interact ray in the Scene view
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Gizmos.DrawWireSphere(ray.origin + ray.direction * interactRange, interactRadius);
    }
}

// Simple helper class to draw spheres in the Game/Scene view for Debugging
public static class DebugExtension
{
    public static void DebugWireSphere(Vector3 origin, Color color, float radius, float duration)
    {
        // This is a simplified visualizer. 
        // Real wire spheres are hard to draw with Debug.DrawLine without a complex loop, 
        // so we usually just rely on the line + console log.
        // But the OnDrawGizmos above will help you see the size in the editor!
    }
}