using FTR.Gameplay.Client.Registry;
using UnityEngine;

public interface IAudioManager
{
    void PlayAtPoint(
        AudioClip clip,
        Vector3 position,
        float priority = 128f,
        float delay = 0f,
        float volume = 1f
    );

    void PlaySoundFXById(
        string soundId,
        Vector3 position,
        float priority = 128f,
        float delay = 0f,
        float volume = 1f
    );

    void PlaySoundFX(SoundFXEntry entry, Vector3 position, float priority = 128f);
}
