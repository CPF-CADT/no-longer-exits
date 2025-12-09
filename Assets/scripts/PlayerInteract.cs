using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 5f; // How close you need to be
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactLayer; 

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            ShootRay();
        }
    }

    void ShootRay()
    {
        // Create a ray starting from the camera's position, shooting forward
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Shoot the ray
        if (Physics.Raycast(ray, out hit, interactRange))
        {
            // Try to find the DoorController on the object we hit OR its parent
            // This fixes your "Parent vs Child" worry automatically
            DoorController door = hit.collider.GetComponentInParent<DoorController>();

            if (door != null)
            {
                door.ToggleDoor();
            }
        }
    }
}