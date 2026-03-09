using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioPersist : MonoBehaviour
{
    private static AudioPersist instance;
    private AudioSource audioSource;

    [SerializeField] private int[] mutedSceneIndices = { 9, 10, 11, 12, 13, 14, 15 };


    // Fade speed controls
    [SerializeField] private float fadeDuration = 1.5f;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (!audioSource.isPlaying)
            audioSource.Play();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int currentIndex = scene.buildIndex;
        bool shouldMute = false;

        foreach (int index in mutedSceneIndices)
        {
            if (currentIndex == index)
            {
                shouldMute = true;
                break;
            }
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Fade out if in muted scenes, fade in otherwise
        fadeCoroutine = StartCoroutine(shouldMute ? FadeOut() : FadeIn());
    }

    private IEnumerator FadeOut()
    {
        float startVolume = audioSource.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Pause();
    }

    private IEnumerator FadeIn()
    {
        audioSource.UnPause();

        float targetVolume = 1f;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}
