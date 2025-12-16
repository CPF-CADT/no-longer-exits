using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] Transform character;

    [Header("Mouse Settings")]
    public float sensitivity = 2f;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;

    // CAMERA FREEZE SWITCH
    [Header("Camera Control")]
    public bool freezeCamera = false;

    // LOOK LIMITS
    [Header("Look Limits")]
    public float minVerticalAngle = -90f; // Look up
    public float maxVerticalAngle = 75f;  // Look down

    // 🔥 HIDE LAYER SETTINGS
    [Header("Hide From This Camera")]
    [Tooltip("Layers that should NOT be rendered by this FPP camera")]
    public LayerMask hiddenLayers;

    Camera cam;

    void Reset()
    {
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = GetComponent<Camera>();

        // 🔥 Apply layer hiding
        ApplyHiddenLayers();
    }

    void Update()
    {
        if (freezeCamera) return;

        // Mouse input
        Vector2 mouseDelta = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );

        Vector2 rawFrameVelocity = mouseDelta * sensitivity;
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1f / smoothing);
        velocity += frameVelocity;

        // Clamp vertical look
        velocity.y = Mathf.Clamp(velocity.y, minVerticalAngle, maxVerticalAngle);

        // Apply rotations
        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    // ==============================
    // 🔥 CAMERA LAYER CONTROL
    // ==============================
    void ApplyHiddenLayers()
    {
        // Remove hidden layers from this camera's culling mask
        cam.cullingMask &= ~hiddenLayers;
    }

    // OPTIONAL: Call this if you change layers at runtime
    public void RefreshCullingMask()
    {
        cam.cullingMask = ~0; // Reset
        ApplyHiddenLayers();
    }
}
