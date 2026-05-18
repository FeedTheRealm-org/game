using System.Collections;
using FTR.Core.Client.Settings;
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

    [SerializeField]
    private SettingsManager settingsManager;

    private static MusicPlayer instance;
    public static MusicPlayer Instance => instance;

    private Coroutine currentFade;
    private bool isInitialized = false;

    private float globalVolumeMultiplier = 1f;
    private float currentBaseVolume = 0.5f;

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

        globalVolumeMultiplier =
            settingsManager != null
                ? (settingsManager.IsMuted ? 0f : settingsManager.MusicVolume)
                : 1f;

        isInitialized = true;
    }

    public void SetGlobalMusicVolume(float globalVolume)
    {
        globalVolumeMultiplier = globalVolume;

        if (musicSource != null && currentFade == null && musicSource.isPlaying)
        {
            musicSource.volume = currentBaseVolume * globalVolumeMultiplier;
        }
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

        currentBaseVolume = volume;

        if (musicSource.isPlaying && musicSource.clip == clip)
        {
            if (!fadeIn)
                musicSource.volume = currentBaseVolume * globalVolumeMultiplier;
            return;
        }

        if (fadeIn)
            currentFade = StartCoroutine(FadeToClip(clip, volume));
        else
        {
            musicSource.clip = clip;
            musicSource.volume = volume * globalVolumeMultiplier;
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
            musicSource.volume =
                Mathf.Lerp(0f, targetVolume, fadeInElapsed / fadeDuration) * globalVolumeMultiplier;
            yield return null;
        }

        musicSource.volume = targetVolume * globalVolumeMultiplier;
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
