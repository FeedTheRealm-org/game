using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicRegistry", menuName = "Scriptable Objects/Audio/Music Registry")]
public class ClientMusicRegistry : ScriptableObject
{
    [SerializeField]
    private MusicEntry menuMusic;

    [SerializeField]
    private MusicEntry worldMusic;

    public MusicEntry MenuMusic => menuMusic;
    public MusicEntry WorldMusic => worldMusic;
}

[Serializable]
public class MusicEntry
{
    [SerializeField]
    private string id;

    [SerializeField]
    private AudioClip[] clips;

    [SerializeField]
    [Range(0f, 1f)]
    private float volume = 0.4f;

    public string Id => id;
    public AudioClip[] Clips => clips;
    public float Volume => volume;
}
