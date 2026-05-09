using System;
using UnityEngine;

namespace FTR.Gameplay.Client.Registry
{
    /// <summary>
    /// Sound data: clip, playback delay, and volume.
    /// </summary>
    [Serializable]
    public class SoundFXEntry
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private AudioClip clip;

        [SerializeField]
        private float delay;

        [SerializeField]
        private float volume = 1f;

        public string Id => id;
        public AudioClip Clip => clip;
        public float Delay => delay;
        public float Volume => volume;
    }
}
