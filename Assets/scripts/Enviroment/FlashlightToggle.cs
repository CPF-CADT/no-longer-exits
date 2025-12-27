using UnityEngine;

public class FlashlightToggle : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag your Flashlight GameObject here (the one with the Light component)")]
    public GameObject flashlightObject;

    [Tooltip("Key to press to turn light On/Off")]
    public KeyCode toggleKey = KeyCode.E;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    void Start()
    {
        // Ensure the light starts in the state you set in the Inspector
        if (flashlightObject == null)
        {
            Debug.LogWarning("Flashlight Object is not assigned!");
        }
    }

    void Update()
    {
        // Check if the specific key is pressed
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleLight();
        }
    }

    void ToggleLight()
    {
        if (flashlightObject != null)
        {
            // Get the current state (true or false)
            bool isActive = flashlightObject.activeSelf;

            // Set it to the opposite state
            flashlightObject.SetActive(!isActive);

            // Play sound if assigned
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }
    }
}