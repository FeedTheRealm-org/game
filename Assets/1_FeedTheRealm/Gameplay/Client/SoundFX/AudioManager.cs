using System.Collections;
using System.Collections.Generic;
using FTR.Gameplay.Client.Registry;
using UnityEngine;
using VContainer;

/// <summary>
/// Audio orchestrator based on AudioSource pool.
/// VContainer injects the ClientSoundFXRegistry via [Inject] Construct().
/// </summary>
public class AudioManager : MonoBehaviour, IAudioManager
{
    [SerializeField]
    private int poolSize = 32;

    [SerializeField]
    private float maxAudibleDistance = 50f;

    private ClientSoundFXRegistry soundFXRegistry;

    private readonly Queue<AudioSource> pool = new Queue<AudioSource>();
    private readonly List<ActiveSound> activeSounds = new List<ActiveSound>();
    private Transform listenerTransform;

    private class ActiveSound
    {
        public AudioSource Source;
        public float Priority;
        public Coroutine ReturnCoroutine;
    }

    [Inject]
    public void Construct(ClientSoundFXRegistry registry)
    {
        soundFXRegistry = registry;
    }

    private void Awake()
    {
        InitPool();
        CacheListener();
    }

    private void InitPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"PooledAudioSource_{i}");
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.maxDistance = maxAudibleDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            go.SetActive(false);
            pool.Enqueue(source);
        }
    }

    private void CacheListener()
    {
        var listenerComponent = FindFirstObjectByType<AudioListener>();
        if (listenerComponent != null)
            listenerTransform = listenerComponent.transform;
        else
            Debug.LogWarning("[AudioManager] No se encontró AudioListener en la escena.");
    }

    /// <summary>
    /// Plays a direct clip. The caller controls delay and volume explicitly.
    /// </summary>
    public void PlayAtPoint(
        AudioClip clip,
        Vector3 position,
        float priority = 128f,
        float delay = 0f,
        float volume = 1f
    )
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Tried to play a null audio clip.");
            return;
        }

        if (
            listenerTransform != null
            && Vector3.Distance(listenerTransform.position, position) > maxAudibleDistance
        )
            return;

        if (delay > 0f)
        {
            StartCoroutine(PlayDelayed(clip, position, priority, delay, volume));
            return;
        }

        PlayImmediate(clip, position, priority, volume);
    }

    /// <summary>
    /// Plays by ID from registry. If delay/volume are not specified (interface defaults),
    /// the values configured in the registry entry are used.
    /// </summary>
    public void PlaySoundFXById(
        string soundId,
        Vector3 position,
        float priority = 128f,
        float delay = 0f,
        float volume = 1f
    )
    {
        if (soundFXRegistry == null)
        {
            Debug.LogWarning("[AudioManager] ClientSoundFXRegistry was not injected.");
            return;
        }

        var entry = soundFXRegistry.GetEntryById(soundId);
        if (entry == null || entry.Clip == null)
        {
            Debug.LogWarning($"[AudioManager] No entry or clip found for id '{soundId}'.");
            return;
        }

        PlaySoundFX(entry, position, priority);
    }

    /// <summary>
    /// Plays an entry using delay y volume configured on registry.
    /// </summary>
    public void PlaySoundFX(SoundFXEntry entry, Vector3 position, float priority = 128f)
    {
        if (entry == null || entry.Clip == null)
        {
            Debug.LogWarning("[AudioManager] Entry is null or has no clip.");
            return;
        }

        PlayAtPoint(entry.Clip, position, priority, entry.Delay, entry.Volume);
    }

    private IEnumerator PlayDelayed(
        AudioClip clip,
        Vector3 position,
        float priority,
        float delay,
        float volume
    )
    {
        yield return new WaitForSeconds(delay);
        PlayImmediate(clip, position, priority, volume);
    }

    private void PlayImmediate(AudioClip clip, Vector3 position, float priority, float volume)
    {
        var source = GetOrStealSource(priority);
        if (source == null)
            return;

        source.gameObject.SetActive(true);
        source.transform.position = position;
        source.priority = (int)priority;
        source.volume = Mathf.Clamp01(volume);
        source.clip = clip;
        source.Play();

        var activeSound = new ActiveSound { Source = source, Priority = priority };
        activeSound.ReturnCoroutine = StartCoroutine(ReturnToPool(activeSound, clip.length));
        activeSounds.Add(activeSound);
    }

    private AudioSource GetOrStealSource(float incomingPriority)
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        ActiveSound lowestPrioritySound = null;
        float lowestPriority = float.MinValue;

        foreach (var active in activeSounds)
        {
            if (active.Priority > lowestPriority)
            {
                lowestPriority = active.Priority;
                lowestPrioritySound = active;
            }
        }

        if (lowestPrioritySound != null && incomingPriority < lowestPrioritySound.Priority)
        {
            StealSource(lowestPrioritySound);
            return pool.Dequeue();
        }

        return null;
    }

    private void StealSource(ActiveSound victim)
    {
        if (victim.ReturnCoroutine != null)
            StopCoroutine(victim.ReturnCoroutine);

        victim.Source.Stop();
        victim.Source.clip = null;
        victim.Source.gameObject.SetActive(false);

        activeSounds.Remove(victim);
        pool.Enqueue(victim.Source);
    }

    private IEnumerator ReturnToPool(ActiveSound activeSound, float clipLength)
    {
        yield return new WaitForSeconds(clipLength);

        activeSound.Source.Stop();
        activeSound.Source.clip = null;
        activeSound.Source.volume = 1f;
        activeSound.Source.gameObject.SetActive(false);

        activeSounds.Remove(activeSound);
        pool.Enqueue(activeSound.Source);
    }
}
