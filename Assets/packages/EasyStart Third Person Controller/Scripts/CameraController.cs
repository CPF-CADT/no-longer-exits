using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("Enable to move the camera by holding the right mouse button. Does not work with joysticks.")]
    public bool clickToMoveCamera = false;
    [Tooltip("Enable zoom in/out when scrolling the mouse wheel.")]
    public bool canZoom = true;
    [Space]
    public float sensitivity = 5f;
    public Vector2 cameraLimit = new Vector2(-45, 40);

    float mouseX;
    float mouseY;
    public float offsetDistanceY;
    public bool freezeCamera = false;

    public Transform player;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        offsetDistanceY = transform.position.y;

        if (!clickToMoveCamera)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // --- BLOCK CAMERA ---
        if (freezeCamera)
        {
            // Prevent any mouse input, zoom, or rotation
            return;
        }

        // --- CAMERA FOLLOW ---
        transform.position = player.position + new Vector3(0, offsetDistanceY, 0);

        // Zoom
        if (canZoom && Input.GetAxis("Mouse ScrollWheel") != 0)
            Camera.main.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * sensitivity * 2;

        // Right-click rotation
        if (clickToMoveCamera && Input.GetAxisRaw("Fire2") == 0) return;

        // Mouse input
        mouseX += Input.GetAxis("Mouse X") * sensitivity;
        mouseY += Input.GetAxis("Mouse Y") * sensitivity;
        mouseY = Mathf.Clamp(mouseY, cameraLimit.x, cameraLimit.y);

        transform.rotation = Quaternion.Euler(-mouseY, mouseX, 0);
    }
}
