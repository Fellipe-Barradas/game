// MusicTrack.cs
using UnityEngine;

[System.Serializable]
public class MusicTrack
{
    public string trackName;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public float fadeInDuration  = 1.5f;
    public float fadeOutDuration = 1.5f;
}