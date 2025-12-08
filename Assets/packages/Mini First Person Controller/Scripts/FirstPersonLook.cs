using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;
    
    // THE SWITCH
    public bool freezeCamera = false; 

    // --- NEW SETTINGS ---
    [Header("Look Limits")]
    public float minVerticalAngle = -90f; // Looking UP (Keep -90 to look at sky)
    public float maxVerticalAngle = 75f;  // Looking DOWN (Reduced from 90 to 75)

    void Reset()
    {
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (freezeCamera) return;

        // Normal Mouse Logic
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;

        // --- THE FIX ---
        // Restrict Y so you can't look straight down at your legs
        // Change 'maxVerticalAngle'i n Inspector to tweak (Try 70 or 75)
        velocity.y = Mathf.Clamp(velocity.y, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }
}