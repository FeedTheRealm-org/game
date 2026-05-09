using FTR.Gameplay.Client.Registry;
using UnityEngine;
using VContainer;

public class SoundPlayer : ISoundPlayer
{
    private readonly IAudioManager audioManager;
    private readonly ClientSoundFXRegistry soundFXRegistry;

    [Inject]
    public SoundPlayer(IAudioManager audioManager, ClientSoundFXRegistry soundFXRegistry)
    {
        this.audioManager = audioManager;
        this.soundFXRegistry = soundFXRegistry;
    }

    public void Play(string soundId, Vector3 position, float priority = 64f)
    {
        var entry = ResolveEntry(soundId);
        if (entry == null)
            return;

        audioManager.PlaySoundFX(entry, position, priority, spatialBlend: 1f);
    }

    public void PlayUI(string soundId, float priority = 64f)
    {
        var entry = ResolveEntry(soundId);
        if (entry == null)
            return;

        audioManager.PlaySoundFX(entry, Vector3.zero, priority, spatialBlend: 0f);
    }

    private SoundFXEntry ResolveEntry(string soundId)
    {
        if (string.IsNullOrEmpty(soundId))
        {
            Debug.LogWarning("[SoundPlayer] Tried to play a null or empty sound ID.");
            return null;
        }

        var entry = soundFXRegistry.GetEntryById(soundId);
        if (entry == null)
        {
            Debug.LogWarning($"[SoundPlayer] No entry found for sound ID '{soundId}'.");
            return null;
        }

        return entry;
    }
}
