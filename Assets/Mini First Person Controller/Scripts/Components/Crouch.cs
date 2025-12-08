using UnityEngine;

public class Crouch : MonoBehaviour
{
    public KeyCode key = KeyCode.LeftControl;

    [Header("Slow Movement")]
    public FirstPersonMovement movement;
    public float movementSpeed = 2;

    [Header("Low Head")]
    public Transform headToLower;
    public float crouchHeight = 1f; // Target height for CharacterController
    public float defaultHeight = 2f; // Normal height for CharacterController
    public float centerOffset = 0.5f; // Adjusts center so feet stay on ground

    CharacterController controller;

    public bool IsCrouched { get; private set; }
    public event System.Action CrouchStart, CrouchEnd;

    void Reset()
    {
        movement = GetComponentInParent<FirstPersonMovement>();
        headToLower = movement.GetComponentInChildren<Camera>().transform;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        // Auto-detect default height if not set
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
        controller.center = new Vector3(0, crouchHeight * centerOffset, 0);

        // 2. Lower the Camera (Visual)
        if (headToLower)
        {
            headToLower.localPosition = new Vector3(headToLower.localPosition.x, 0.5f, headToLower.localPosition.z);
        }

        // 3. Slow Down
        SetSpeedOverrideActive(true);
        CrouchStart?.Invoke();
    }

    void StopCrouch()
    {
        IsCrouched = false;

        // 1. Reset Controller
        controller.height = defaultHeight;
        controller.center = new Vector3(0, defaultHeight * 0.5f, 0);

        // 2. Reset Camera
        if (headToLower)
        {
            headToLower.localPosition = new Vector3(headToLower.localPosition.x, 1.6f, headToLower.localPosition.z); // Assuming 1.6 is eye level
        }

        // 3. Reset Speed
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