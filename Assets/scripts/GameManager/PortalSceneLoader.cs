using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PortalSceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load (must be added to Build Settings)")]
    public string sceneToLoad;

    [Tooltip("Delay before scene loads")]
    public float loadDelay = 0.5f;

    [Header("Player Detection")]
    [Tooltip("Tag used by the player")]
    public string playerTag = "Player";

    [Header("Optional Effects")]
    public AudioSource enterSound;
    public GameObject enterVFX;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;
            StartCoroutine(LoadSceneRoutine());
        }
    }

    private IEnumerator LoadSceneRoutine()
    {
        // Play sound
        if (enterSound != null)
            enterSound.Play();

        // Spawn VFX
        if (enterVFX != null)
            Instantiate(enterVFX, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(loadDelay);

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("[PortalSceneLoader] Scene name is empty!");
        }
    }
}
