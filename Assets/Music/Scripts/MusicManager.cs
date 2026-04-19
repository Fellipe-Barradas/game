// MusicManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static MusicManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string mixerVolumeParam = "MusicVolume"; // parâmetro exposto

    [Header("Crossfade")]
    [SerializeField] private float defaultFadeDuration = 1.5f;

    [Header("Configs de cenas (ordem importa)")]
    [SerializeField] private SceneMusicConfig[] sceneMusicConfigs;

    // ── Privados ──────────────────────────────────────────────────────────────
    private AudioSource[] _sources = new AudioSource[2]; // A e B para crossfade
    private int _activeIndex = 0;                        // qual source está ativo

    private SceneMusicConfig _currentConfig;
    private int              _playlistIndex  = -1;
    private List<int>        _shuffleOrder   = new();
    private Coroutine        _fadeCoroutine;
    private Coroutine        _playlistCoroutine;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateAudioSources();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // ── Criação das AudioSources ──────────────────────────────────────────────
    private void CreateAudioSources()
    {
        for (int i = 0; i < 2; i++)
        {
            _sources[i] = gameObject.AddComponent<AudioSource>();
            _sources[i].loop        = false;
            _sources[i].playOnAwake = false;
            _sources[i].outputAudioMixerGroup =
                audioMixer.FindMatchingGroups("Music")[0]; // grupo "Music" no mixer
        }
    }

    // ── Detecção de cena ──────────────────────────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneMusicConfig config = FindConfigForScene(scene.name);
        if (config != null) PlayConfig(config);
    }

    private SceneMusicConfig FindConfigForScene(string sceneName)
    {
        foreach (var cfg in sceneMusicConfigs)
            if (cfg.sceneKey == sceneName) return cfg;
        return null;
    }

    // ── API Pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Toca uma config manualmente (útil para áreas dentro de uma mesma cena).
    /// </summary>
    public void PlayConfig(SceneMusicConfig config)
    {
        if (config == null || config.playlist.Length == 0) return;
        if (_currentConfig == config) return;   // mesma config → não reinicia

        _currentConfig = config;
        _playlistIndex = -1;
        BuildShuffleOrder(config.playlist.Length);

        StopPlaylistCoroutine();
        _playlistCoroutine = StartCoroutine(PlaylistRoutine());
    }

    /// <summary>Toca uma trilha específica com crossfade.</summary>
    public void PlayTrack(MusicTrack track)
    {
        StopPlaylistCoroutine();
        StopFade();
        _fadeCoroutine = StartCoroutine(CrossfadeTo(track));
    }

    public void StopMusic(float fadeDuration = -1f)
    {
        StopPlaylistCoroutine();
        StopFade();
        float dur = fadeDuration < 0 ? defaultFadeDuration : fadeDuration;
        _fadeCoroutine = StartCoroutine(FadeOutActive(dur));
    }

    public void SetVolume(float normalizedVolume)
    {
        // Converte 0-1 para dB (log), com floor em -80 dB
        float dB = normalizedVolume > 0.0001f
            ? Mathf.Log10(normalizedVolume) * 20f
            : -80f;
        audioMixer.SetFloat(mixerVolumeParam, dB);
    }

    // ── Playlist Coroutine ────────────────────────────────────────────────────
    private IEnumerator PlaylistRoutine()
    {
        while (_currentConfig != null && _currentConfig.playlist.Length > 0)
        {
            MusicTrack next = GetNextTrack();
            yield return _fadeCoroutine = StartCoroutine(CrossfadeTo(next));

            // Espera a faixa terminar (menos o tempo do fade de saída)
            AudioSource active = _sources[_activeIndex];
            float waitTime = active.clip.length
                             - active.time
                             - next.fadeOutDuration;

            if (waitTime > 0) yield return new WaitForSeconds(waitTime);

            if (!_currentConfig.loop && IsLastTrack()) yield break;
        }
    }

    private MusicTrack GetNextTrack()
    {
        var playlist = _currentConfig.playlist;

        if (_currentConfig.shuffle)
        {
            _playlistIndex = (_playlistIndex + 1) % _shuffleOrder.Count;
            return playlist[_shuffleOrder[_playlistIndex]];
        }
        else
        {
            _playlistIndex = (_playlistIndex + 1) % playlist.Length;
            return playlist[_playlistIndex];
        }
    }

    private bool IsLastTrack()
    {
        return _playlistIndex >= _currentConfig.playlist.Length - 1;
    }

    private void BuildShuffleOrder(int count)
    {
        _shuffleOrder.Clear();
        for (int i = 0; i < count; i++) _shuffleOrder.Add(i);
        // Fisher-Yates
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
        }
    }

    // ── Crossfade Coroutine ───────────────────────────────────────────────────
    private IEnumerator CrossfadeTo(MusicTrack track)
    {
        int nextIndex = 1 - _activeIndex;
        AudioSource fadeIn  = _sources[nextIndex];
        AudioSource fadeOut = _sources[_activeIndex];

        // Prepara a nova faixa
        fadeIn.clip   = track.clip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        float fadeDuration = track.fadeInDuration > 0
            ? track.fadeInDuration
            : defaultFadeDuration;

        float elapsed   = 0f;
        float startVol  = fadeOut.volume;
        float targetVol = track.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            fadeIn.volume  = Mathf.Lerp(0f,        targetVol, t);
            fadeOut.volume = Mathf.Lerp(startVol,  0f,        t);

            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;
        _activeIndex   = nextIndex;
    }

    private IEnumerator FadeOutActive(float duration)
    {
        AudioSource active = _sources[_activeIndex];
        float startVol = active.volume;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed       += Time.deltaTime;
            active.volume  = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        active.Stop();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void StopFade()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
    }

    private void StopPlaylistCoroutine()
    {
        if (_playlistCoroutine != null) StopCoroutine(_playlistCoroutine);
    }
}