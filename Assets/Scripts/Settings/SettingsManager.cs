using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Áudio")]
    [Tooltip("AudioMixer principal (master). Deve ter exposto um parâmetro \"MasterVolume\".")]
    public AudioMixer masterMixer;

    [Range(0f, 1f)]
    public float masterVolume = 1f;

    public bool isMuted = false;

    [Header("Vídeo")]
    public bool isFullscreen = true;
    public int currentResolutionIndex = 0;
    public Resolution[] availableResolutions;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // carregar resoluções disponíveis
        availableResolutions = Screen.resolutions;

        // carregar prefs guardados (se quiseres)
        LoadSettings();

        // aplicar logo ao arrancar
        ApplyVolume();
        ApplyResolution();
    }

    public void SetMasterVolume(float value01)
    {
        masterVolume = Mathf.Clamp01(value01);
        ApplyVolume();
        SaveSettings();
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        ApplyVolume();
        SaveSettings();
    }

    private void ApplyVolume()
    {
        if (masterMixer == null)
            return;

        float volume = isMuted ? 0f : masterVolume;

        // converter linear [0..1] em dB (-80..0)
        float db;
        if (volume <= 0.0001f)
            db = -80f;
        else
            db = Mathf.Lerp(-30f, 0f, volume); // curva simples

        masterMixer.SetFloat("MasterVolume", db);
    }

    public void SetResolution(int index, bool fullscreen)
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, availableResolutions.Length - 1);
        currentResolutionIndex = index;
        isFullscreen = fullscreen;

        ApplyResolution();
        SaveSettings();
    }

    private void ApplyResolution()
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
            availableResolutions = Screen.resolutions;

        if (availableResolutions.Length == 0)
            return;

        Resolution res = availableResolutions[currentResolutionIndex];
        Screen.SetResolution(res.width, res.height, isFullscreen);
    }

    public void SetFullscreen(bool fullscreen)
    {
        isFullscreen = fullscreen;
        Screen.fullScreen = isFullscreen;
        SaveSettings();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("settings_masterVolume", masterVolume);
        PlayerPrefs.SetInt("settings_muted", isMuted ? 1 : 0);
        PlayerPrefs.SetInt("settings_fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("settings_resIndex", currentResolutionIndex);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("settings_masterVolume"))
            masterVolume = PlayerPrefs.GetFloat("settings_masterVolume", 1f);

        isMuted = PlayerPrefs.GetInt("settings_muted", 0) == 1;
        isFullscreen = PlayerPrefs.GetInt("settings_fullscreen", 1) == 1;

        availableResolutions = Screen.resolutions;
        currentResolutionIndex = Mathf.Clamp(
            PlayerPrefs.GetInt("settings_resIndex", availableResolutions.Length - 1),
            0,
            availableResolutions.Length - 1);
    }
}