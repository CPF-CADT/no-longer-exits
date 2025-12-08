using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody))]
public class FirstPersonAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Rigidbody rb;

    [Header("Dependencies")]
    public FirstPersonMovement movementScript;
    public Crouch crouchScript;

    [Tooltip("Drag your GroundCheck object here (the one with the GroundCheck script)")]
    public GroundCheck groundChecker;

    // Parameters
    private const float minMoveSpeed = 0.1f;

    void Reset()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        movementScript = GetComponent<FirstPersonMovement>();
        crouchScript = GetComponent<Crouch>();
        groundChecker = GetComponentInChildren<GroundCheck>();
    }

    void Update()
    {
        if (animator == null || rb == null) return;

        // --- 1. GATHER DATA ---
        
        // Check Ground Status (Default to true if no checker assigned to avoid getting stuck in jump anim)
        bool isGrounded = groundChecker != null ? groundChecker.isGrounded : true;
        
        // Calculate Horizontal Speed (Ignore Y/Falling speed)
        Vector3 horizontalVelocity = rb.velocity;
        horizontalVelocity.y = 0;
        float currentSpeed = horizontalVelocity.magnitude;

        // Check Crouch Status
        bool isCrouching = crouchScript != null && crouchScript.IsCrouched;

        // Check Sprint Status (Must be moving fast enough + Input is true + NOT Crouching)
        bool isSprinting = (currentSpeed > minMoveSpeed) 
                            && (movementScript != null && movementScript.IsRunning) 
                            && !isCrouching;

        // Check Walk/Run Status (Moving + NOT Sprinting)
        // Note: In some Animators, you keep "Run" true while sprinting. 
        // If your transitions are "Idle -> Walk -> Sprint", use the logic below:
        bool isWalking = (currentSpeed > minMoveSpeed) && !isSprinting;


        // --- 2. SET ANIMATOR VALUES ---

        // GROUNDED LOGIC
        if (isGrounded)
        {
            animator.SetBool("air", false);
            animator.SetBool("crouch", isCrouching);
            
            // We set 'run' to true if we are moving (walking or sprinting)
            // Ideally, rename your Animator parameter to "isMoving" or use a Float for Speed.
            // Based on your previous code, 'run' likely means "Moving Forward".
            animator.SetBool("run", currentSpeed > minMoveSpeed); 
            
            animator.SetBool("sprint", isSprinting);
        }
        else
        {
            // AIR LOGIC
            animator.SetBool("air", true);
            
            // Optional: Force movement bools off while in air to prevent "air-walking"
            // (Unless you want Quake/Source engine style air-strafing visuals)
            animator.SetBool("run", false);
            animator.SetBool("sprint", false);
            
            // We usually allow crouching in air (Crouch Jumping)
            animator.SetBool("crouch", isCrouching);
        }
    }
}