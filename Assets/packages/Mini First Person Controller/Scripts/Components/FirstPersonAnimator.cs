using UnityEngine;

public class FirstPersonAnimator : MonoBehaviour
{
    [Header("References")]
    // This variable was likely missing or named differently in your script
    public Animator animator; 
    public FirstPersonMovement movementScript;
    public Crouch crouchScript;

    // Internal reference
    private CharacterController controller;

    void Start()
    {
        // --- AUTO-FIND LOGIC (Fixes the "Cannot Drop" issue) ---
        
        // 1. Look for Animator inside children (like your "helmet.002" or mesh)
        if (animator == null) 
            animator = GetComponentInChildren<Animator>();

        // 2. Find scripts on this object
        if (movementScript == null) 
            movementScript = GetComponent<FirstPersonMovement>();
            
        if (crouchScript == null) 
            crouchScript = GetComponent<Crouch>();
        
        // 3. Get the controller
        controller = GetComponent<CharacterController>();

        // Debug check
        if (animator == null) Debug.LogError("Still cannot find an Animator! Make sure your 3D model is a child of this object.");
    }

    void Update()
    {
        // Safety check: stop if we are missing components
        if (animator == null || movementScript == null || controller == null) return;

        // --- 1. GET DATA ---
        
        // Use Controller velocity (not Rigidbody)
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0; 
        float currentSpeed = horizontalVelocity.magnitude;

        bool isGrounded = movementScript.isGrounded;
        bool isCrouching = crouchScript != null && crouchScript.IsCrouched;
        bool isSprinting = movementScript.IsRunning && currentSpeed > 0.1f;

        // --- 2. SET ANIMATOR PARAMETERS ---
        
        // Using the parameter names from your previous screenshot
        animator.SetBool("air", !isGrounded);
        animator.SetBool("crouch", isCrouching);
        animator.SetBool("run", currentSpeed > 0.1f);
        animator.SetBool("sprint", isSprinting);
    }
}