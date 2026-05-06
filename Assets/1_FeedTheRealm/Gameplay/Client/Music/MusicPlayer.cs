using System.Collections;
using UnityEngine;

public enum MusicType
{
    Menu,
    World,
}

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [SerializeField]
    private AudioSource musicSource;

    [SerializeField]
    private float fadeDuration = 1.5f;

    private static MusicPlayer instance;
    public static MusicPlayer Instance => instance;

    private Coroutine currentFade;
    private bool isInitialized = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0f;

        isInitialized = true;
    }

    public void Initialize(ClientMusicRegistry registry, MusicType type)
    {
        if (!isInitialized || registry == null)
            return;

        MusicEntry entry = type == MusicType.Menu ? registry.MenuMusic : registry.WorldMusic;

        if (entry?.Clip != null)
            Play(entry.Clip, entry.Volume);
    }

    public void Play(AudioClip clip, float volume = 0.5f, bool fadeIn = true)
    {
        if (!isInitialized || clip == null)
            return;

        if (currentFade != null)
            StopCoroutine(currentFade);

        if (musicSource.isPlaying && musicSource.clip == clip)
            return;

        if (fadeIn)
            currentFade = StartCoroutine(FadeToClip(clip, volume));
        else
        {
            musicSource.clip = clip;
            musicSource.volume = volume;
            musicSource.Play();
        }
    }

    public void DestroyInstance(bool fadeOut = true)
    {
        if (!isInitialized)
        {
            Destroy(gameObject);
            return;
        }

        if (fadeOut && musicSource.isPlaying)
        {
            if (currentFade != null)
                StopCoroutine(currentFade);
            StartCoroutine(FadeOutAndDestroy());
        }
        else
        {
            CleanupAndDestroy();
        }
    }

    private void CleanupAndDestroy()
    {
        instance = null;
        Destroy(gameObject);
    }

    private IEnumerator FadeToClip(AudioClip newClip, float targetVolume)
    {
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        float fadeInElapsed = 0f;
        while (fadeInElapsed < fadeDuration)
        {
            fadeInElapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, fadeInElapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        currentFade = null;
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        CleanupAndDestroy();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
