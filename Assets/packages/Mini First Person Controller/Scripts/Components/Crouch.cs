using UnityEngine;

public class Crouch : MonoBehaviour
{
    public KeyCode key = KeyCode.LeftControl;

    [Header("Slow Movement")]
    public FirstPersonMovement movement;
    public float movementSpeed = 2;

    [Header("Low Head")]
    public Transform headToLower;
    [Tooltip("How tall the player is when crouching")]
    public float crouchHeight = 1f; 
    
    private float defaultHeight;
    private CharacterController controller;

    public bool IsCrouched { get; private set; }
    public event System.Action CrouchStart, CrouchEnd;

    void Reset()
    {
        movement = GetComponentInParent<FirstPersonMovement>();
        // Try to find the camera automatically
        if (movement != null) headToLower = movement.GetComponentInChildren<Camera>().transform;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        defaultHeight = controller.height;
    }

    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            StartCrouch();
        }
        else if (Input.GetKeyUp(key))
        {
            StopCrouch();
        }
    }

    void StartCrouch()
    {
        IsCrouched = true;

        // 1. Lower the Controller
        controller.height = crouchHeight;
        
        // FIX: Automatically calculate center so feet remain at Y=0
        // Center is always half of the height
        controller.center = new Vector3(0, crouchHeight / 2f, 0);

        // 2. Lower the Camera
        if (headToLower)
        {
            // Move camera down to match the new eye level (e.g. 0.6 instead of 0.5 to prevent clipping near floor)
            headToLower.localPosition = new Vector3(headToLower.localPosition.x, crouchHeight * 0.8f, headToLower.localPosition.z);
        }

        // 3. Slow Down
        SetSpeedOverrideActive(true);
        CrouchStart?.Invoke();
    }

    void StopCrouch()
    {
        // 1. Check if there is room to stand up (Optional safety)
        // If you are under a vent, you shouldn't be able to stand up.
        // For now, we force stand up to fix your issue.

        IsCrouched = false;

        // 2. Reset Controller
        controller.height = defaultHeight;
        controller.center = new Vector3(0, defaultHeight / 2f, 0);

        // 3. Reset Camera
        if (headToLower)
        {
            // Move camera back to original eye level (approx 0.8 * height)
            headToLower.localPosition = new Vector3(headToLower.localPosition.x, defaultHeight * 0.8f, headToLower.localPosition.z);
        }

        // 4. Reset Speed
        SetSpeedOverrideActive(false);
        CrouchEnd?.Invoke();
    }

    #region Speed override
    void SetSpeedOverrideActive(bool state)
    {
        if(!movement) return;

        if (state)
        {
            if (!movement.speedOverrides.Contains(SpeedOverride))
                movement.speedOverrides.Add(SpeedOverride);
        }
        else
        {
            if (movement.speedOverrides.Contains(SpeedOverride))
                movement.speedOverrides.Remove(SpeedOverride);
        }
    }

    float SpeedOverride() => movementSpeed;
    #endregion
}