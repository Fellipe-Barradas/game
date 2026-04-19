// VolumeSliderUI.cs
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSliderUI : MonoBehaviour
{
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY  = "MusicVolume";

    [SerializeField] private Slider     masterSlider;
    [SerializeField] private Slider     musicSlider;
    [SerializeField] private AudioMixer audioMixer;   // arraste o GameAudioMixer aqui

    private void Start()
    {
        // Restaura valores salvos (padrão 0.75 para ambos)
        float savedMaster = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0.75f);
        float savedMusic  = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY,  0.75f);

        masterSlider.value = savedMaster;
        musicSlider.value  = savedMusic;

        ApplyMasterVolume(savedMaster);
        ApplyMusicVolume(savedMusic);

        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
    }

    private void OnDestroy()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
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

    // Converte 0-1 para dB e aplica no mixer
    private void ApplyMasterVolume(float normalized)
    {
        audioMixer.SetFloat("MasterVolume", NormalizedToDB(normalized));
    }

    private void ApplyMusicVolume(float normalized)
    {
        audioMixer.SetFloat("MusicVolume", NormalizedToDB(normalized));
    }

    private float NormalizedToDB(float value)
    {
        return value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
    }
}