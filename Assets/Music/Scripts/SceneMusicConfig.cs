// SceneMusicConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "SceneMusicConfig", menuName = "Audio/Scene Music Config")]
public class SceneMusicConfig : ScriptableObject
{
    [Tooltip("Nome da cena ou área (ex: 'MainMenu', 'Forest', 'Cave')")]
    public string sceneKey;

    public MusicTrack[] playlist;
    public bool shuffle = false;
    public bool loop    = true;   // loop da playlist inteira
}