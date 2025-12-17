using UnityEngine;

public class SmoothCameraLook : MonoBehaviour
{
    [Header("Settings")]
    public float sensitivity = 2.0f;
    public float smoothing = 1.5f; // Higher = smoother, Lower = more snappy

    [Header("References")]
    public Transform playerBody; // Drag your Player object here

    private Vector2 _currentMouseLook;
    private Vector2 _appliedMouseDelta;
    private Vector2 _smoothV;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Get raw input
        Vector2 targetMouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // 2. Smooth the input (This fixes the robotic/trembling feel)
        _appliedMouseDelta = Vector2.SmoothDamp(_appliedMouseDelta, targetMouseDelta, ref _smoothV, 1f / smoothing * 0.1f);

        _currentMouseLook += _appliedMouseDelta * sensitivity;

        // 3. Clamp looking up/down
        _currentMouseLook.y = Mathf.Clamp(_currentMouseLook.y, -90f, 90f);
    }

    // 4. LateUpdate ensures the body has finished moving before the camera rotates
    void LateUpdate()
    {
        // Rotate the Camera (Up/Down)
        transform.localRotation = Quaternion.AngleAxis(-_currentMouseLook.y, Vector3.right);

        // Rotate the Player Body (Left/Right)
        if (playerBody != null)
        {
            playerBody.localRotation = Quaternion.AngleAxis(_currentMouseLook.x, Vector3.up);
        }
    }
}