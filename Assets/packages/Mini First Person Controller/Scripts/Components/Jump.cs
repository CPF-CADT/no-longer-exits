using UnityEngine;

public class Jump : MonoBehaviour
{
    public float jumpStrength = 8f; // Adjusted for CharacterController physics
    public event System.Action Jumped;

    FirstPersonMovement movement;

    void Awake()
    {
        movement = GetComponent<FirstPersonMovement>();
    }

    void Update()
    {
        // We use movement.isGrounded from the new script
        if (Input.GetButtonDown("Jump") && movement.isGrounded)
        {
            // Apply the jump force directly to the movement script
            movement.SetVerticalVelocity(jumpStrength);
            Jumped?.Invoke();
        }
    }
}