using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndingSceneManager : MonoBehaviour
{
    [System.Serializable]
    public class Scene
    {
        public Sprite image;
        [TextArea] public string text;
        public float duration = 3f;
    }

    [Header("Scenes")]
    public Scene[] scenes;

    [Header("UI References")]
    public Image sceneImage;
    public TextMeshProUGUI sceneText;
    public Image textBackground;

    [Header("Timing")]
    public float fadeDuration = 1f;
    public float skipFadeDuration = 0.25f;

    private int currentScene = 0;
    private Coroutine sceneCoroutine;
    private bool isSkipping = false;

    void Start()
    {
        SetUIAlpha(0f);

        if (scenes.Length > 0)
            sceneCoroutine = StartCoroutine(PlayScenes());
    }

    void Update()
    {
        if (Input.anyKeyDown && !isSkipping)
        {
            StartCoroutine(SkipScene());
        }
    }

    IEnumerator PlayScenes()
    {
        while (currentScene < scenes.Length)
        {
            Scene scene = scenes[currentScene];

            sceneImage.sprite = scene.image;
            sceneText.text = scene.text;

            // Fade in (dark → bright)
            yield return StartCoroutine(FadeUI(0f, 1f, fadeDuration));

            // Stay visible
            yield return new WaitForSeconds(scene.duration - fadeDuration);

            // Fade out (bright → dark)
            yield return StartCoroutine(FadeUI(1f, 0f, fadeDuration));

            currentScene++;
        }

        Debug.Log("Ending scene finished");
    }

    IEnumerator SkipScene()
    {
        isSkipping = true;

        if (sceneCoroutine != null)
            StopCoroutine(sceneCoroutine);

        // Force current scene fully visible
        SetUIAlpha(1f);

        // Fade bright → dark quickly
        yield return StartCoroutine(FadeUI(1f, 0f, skipFadeDuration));

        currentScene++;

        if (currentScene < scenes.Length)
        {
            // Prepare next scene fully visible (NO fade in)
            sceneImage.sprite = scenes[currentScene].image;
            sceneText.text = scenes[currentScene].text;
            SetUIAlpha(1f);

            sceneCoroutine = StartCoroutine(PlayScenesFromVisible());
        }
        else
        {
            Debug.Log("Ending scene finished");
            SceneManager.LoadScene("Startmenu");
        }

        isSkipping = false;
    }

    IEnumerator PlayScenesFromVisible()
    {
        while (currentScene < scenes.Length)
        {
            Scene scene = scenes[currentScene];

            sceneImage.sprite = scene.image;
            sceneText.text = scene.text;

            // Already visible, just wait
            yield return new WaitForSeconds(scene.duration - fadeDuration);

            // Fade out
            yield return StartCoroutine(FadeUI(1f, 0f, fadeDuration));

            currentScene++;

            if (currentScene < scenes.Length)
            {
                sceneImage.sprite = scenes[currentScene].image;
                sceneText.text = scenes[currentScene].text;

                // Next scene starts bright
                SetUIAlpha(1f);
            }
        }
    }

    IEnumerator FadeUI(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            SetUIAlpha(a);
            yield return null;
        }

        SetUIAlpha(to);
    }

    void SetUIAlpha(float alpha)
    {
        sceneImage.color = new Color(1f, 1f, 1f, alpha);
        sceneText.color = new Color(1f, 1f, 1f, alpha);
        textBackground.color = new Color(0f, 0f, 0f, alpha * 0.8f);
    }
}
