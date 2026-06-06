// VolumeSliderUI.cs
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSliderUI : MonoBehaviour
{
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY  = "MusicVolume";
    private const string PLAYER_VOLUME_KEY = "PlayerVolume";

    [SerializeField] private Slider     masterSlider;
    [SerializeField] private Slider     musicSlider;
    [SerializeField] private Slider     playerSlider;
    [SerializeField] private AudioMixer audioMixer;

    private void Start()
    {
        float savedMaster = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0.75f);
        float savedMusic  = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY,  0.75f);
        float savedPlayer = PlayerPrefs.GetFloat(PLAYER_VOLUME_KEY, 0.75f);

        masterSlider.value = savedMaster;
        musicSlider.value  = savedMusic;
        if (playerSlider != null) playerSlider.value = savedPlayer;

        ApplyMasterVolume(savedMaster);
        ApplyMusicVolume(savedMusic);
        ApplyPlayerVolume(savedPlayer);

        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        if (playerSlider != null) playerSlider.onValueChanged.AddListener(OnPlayerSliderChanged);
    }

    private void OnDestroy()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (playerSlider != null) playerSlider.onValueChanged.RemoveListener(OnPlayerSliderChanged);
    }

    private void OnMasterSliderChanged(float value)
    {
        ApplyMasterVolume(value);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
        PlayerPrefs.Save();
    }

    private void OnMusicSliderChanged(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
        PlayerPrefs.Save();
    }

    private void OnPlayerSliderChanged(float value)
    {
        ApplyPlayerVolume(value);
        PlayerPrefs.SetFloat(PLAYER_VOLUME_KEY, value);
        PlayerPrefs.Save();
    }

    private void ApplyMasterVolume(float normalized)
    {
        audioMixer.SetFloat("MasterVolume", NormalizedToDB(normalized));
    }

    private void ApplyMusicVolume(float normalized)
    {
        audioMixer.SetFloat("MusicVolume", NormalizedToDB(normalized));
    }

    private void ApplyPlayerVolume(float normalized)
    {
        audioMixer.SetFloat("PlayerVolume", NormalizedToDB(normalized));
    }

    private float NormalizedToDB(float value)
    {
        return value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
    }
}