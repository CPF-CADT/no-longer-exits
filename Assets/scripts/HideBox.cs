using UnityEngine;
using System.Collections.Generic;

public class HideBox : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactionDistance = 2f;

    [Header("Player")]
    public Transform player;
    public GameObject playerModel;

    [Header("Cameras")]
    [Tooltip("Main player camera (FPP / TPS)")]
    public Camera playerCamera;

    [Tooltip("Camera used while hiding inside the box")]
    public Camera hideCamera;

    private ThirdPersonController controller;
    private bool isPlayerHidden = false;
    private Vector3 originalPlayerPosition;

    // Track all hide boxes
    private static List<HideBox> allHideBoxes = new List<HideBox>();

    void OnEnable() => allHideBoxes.Add(this);
    void OnDisable() => allHideBoxes.Remove(this);

    void Start()
    {
        if (player != null)
            controller = player.GetComponent<ThirdPersonController>();

        if (playerModel == null && player != null)
            playerModel = player.GetComponentInChildren<SkinnedMeshRenderer>()?.gameObject;

        // Ensure correct camera state at start
        if (hideCamera != null)
            hideCamera.enabled = false;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactionDistance && Input.GetKeyDown(interactKey))
        {
            ToggleHide();
        }
    }

    private void ToggleHide()
    {
        isPlayerHidden = !isPlayerHidden;

        if (isPlayerHidden)
        {
            EnterHide();
        }
        else
        {
            ExitHide();
        }
    }

    private void EnterHide()
    {
        originalPlayerPosition = player.position;
        player.position = transform.position;

        // Hide player visuals only (NOT the camera)
        if (playerModel != null) playerModel.SetActive(false);

        // Disable movement controller
        if (controller != null) controller.enabled = false;

        // Disable player colliders
        foreach (Collider col in player.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Camera switch
        if (playerCamera != null) playerCamera.enabled = false;
        if (hideCamera != null) hideCamera.enabled = true;

        Debug.Log("Player is hidden in the box!");
    }

    private void ExitHide()
    {
        player.position = originalPlayerPosition;

        if (playerModel != null) playerModel.SetActive(true);
        if (controller != null) controller.enabled = true;

        foreach (Collider col in player.GetComponentsInChildren<Collider>())
            col.enabled = true;

        // Camera switch back
        if (hideCamera != null) hideCamera.enabled = false;
        if (playerCamera != null) playerCamera.enabled = true;

        Debug.Log("Player exited the box!");
    }

    // Public getter
    public bool IsPlayerHidden => isPlayerHidden;

    // Check if player is hidden anywhere
    public static bool IsPlayerHiddenAnywhere()
    {
        foreach (var box in allHideBoxes)
            if (box.IsPlayerHidden) return true;
        return false;
    }
}
