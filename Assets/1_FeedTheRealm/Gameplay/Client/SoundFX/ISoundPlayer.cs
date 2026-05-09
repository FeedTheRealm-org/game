using FTR.Gameplay.Client.Registry;
using UnityEngine;

public interface ISoundPlayer
{
    /// <summary>
    /// Plays a 3D spatial sound at a world position (footsteps, combat, environment).
    /// </summary>
    void Play(string soundId, Vector3 position, float priority = 64f);

    /// <summary>
    /// Plays a 2D/UI sound globally (menus, popups, notifications). Ignores listener position.
    /// </summary>
    void PlayUI(string soundId, float priority = 64f);
}
