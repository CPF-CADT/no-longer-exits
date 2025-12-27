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
    public Camera playerCamera;
    public Camera hideCamera;
    private InteractionHint hint;

    private AudioListener playerListener;
    private AudioListener hideListener;

    private ThirdPersonController controller;
    private bool isPlayerHidden = false;
    private Vector3 originalPlayerPosition;

    private static List<HideBox> allHideBoxes = new List<HideBox>();

    void OnEnable() => allHideBoxes.Add(this);
    void OnDisable() => allHideBoxes.Remove(this);

    void Awake()
    {
        // Get hint on this GameObject or child
        hint = GetComponentInChildren<InteractionHint>(true);

        if (hint == null)
            Debug.LogError($"{name} is missing InteractionHint component");
    }

    void Start()
    {
        if (player != null)
            controller = player.GetComponent<ThirdPersonController>();

        if (playerModel == null && player != null)
            playerModel = player.GetComponentInChildren<SkinnedMeshRenderer>()?.gameObject;

        if (playerCamera != null)
            playerListener = playerCamera.GetComponent<AudioListener>();

        if (hideCamera != null)
        {
            hideCamera.enabled = false;
            hideListener = hideCamera.GetComponent<AudioListener>();
            if (hideListener != null) hideListener.enabled = false;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactionDistance && Input.GetKeyDown(interactKey))
        {
            ToggleHide();
        }

        hint.useAlternate = isPlayerHidden;
    }

    public void ToggleHide()
    {
        isPlayerHidden = !isPlayerHidden;

        if (isPlayerHidden) EnterHide();
        else ExitHide();
    }

    private void EnterHide()
    {
        originalPlayerPosition = player.position;
        player.position = transform.position;

        if (playerModel != null) playerModel.SetActive(false);
        if (controller != null) controller.enabled = false;

        foreach (Collider col in player.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Camera + Audio
        if (playerCamera != null) playerCamera.enabled = false;
        if (playerListener != null) playerListener.enabled = false;

        if (hideCamera != null) hideCamera.enabled = true;
        if (hideListener != null) hideListener.enabled = true;

        Debug.Log("Player hidden — hide camera + audio listener active");
    }

    private void ExitHide()
    {
        player.position = originalPlayerPosition;

        if (playerModel != null) playerModel.SetActive(true);
        if (controller != null) controller.enabled = true;

        foreach (Collider col in player.GetComponentsInChildren<Collider>())
            col.enabled = true;

        // Camera + Audio back
        if (hideCamera != null) hideCamera.enabled = false;
        if (hideListener != null) hideListener.enabled = false;

        if (playerCamera != null) playerCamera.enabled = true;
        if (playerListener != null) playerListener.enabled = true;

        Debug.Log("Player exited — player camera + audio restored");
    }

    public bool IsPlayerHidden => isPlayerHidden;

    public static bool IsPlayerHiddenAnywhere()
    {
        foreach (var box in allHideBoxes)
            if (box.IsPlayerHidden) return true;
        return false;
    }
}
