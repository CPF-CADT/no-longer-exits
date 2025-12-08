using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Physics")]
    public float gravity = -9.81f;
    
    // Internal variables
    CharacterController controller;
    Vector3 verticalVelocity; // Stores force for gravity and jumping
    
    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. RUNNING LOGIC
        IsRunning = canRun && Input.GetKey(runningKey);

        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // 2. MOVEMENT (Horizontal)
        // We use transform.right/forward so movement is relative to where we look
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        
        // Apply horizontal movement
        controller.Move(move * targetMovingSpeed * Time.deltaTime);

        // 3. GRAVITY (Vertical)
        // If we are on the ground, reset our downward pull so it doesn't build up infinite speed
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // Small force to keep us stuck to floor
        }

        // Apply gravity over time
        verticalVelocity.y += gravity * Time.deltaTime;

        // Apply vertical movement
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // This function allows other scripts (like Jump) to push the character up
    public void SetVerticalVelocity(float force)
    {
        verticalVelocity.y = force;
    }

    // Helper property so other scripts know if we are grounded
    public bool isGrounded => controller.isGrounded;
}