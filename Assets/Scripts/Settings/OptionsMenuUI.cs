using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class OptionsMenuUI : MonoBehaviour
{
    [Header("Áudio")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider voiceVolumeSlider;

    [Tooltip("Mute apenas Música (mantém o valor do slider).")]
    public Toggle muteMusicToggle;

    [Tooltip("Mute apenas SFX (mantém o valor do slider).")]
    public Toggle muteSfxToggle;

    [Tooltip("Mute apenas Voz (mantém o valor do slider).")]
    public Toggle muteVoiceToggle;

    [Tooltip("Quando ligado, o jogo fica totalmente sem som (música, SFX e voz).")]
    public Toggle muteAllToggle;

    [Header("Voz - Idioma")]
    public TMP_Dropdown languageDropdown; // ¡Aquí está el nuevo Dropdown!

    [Header("Voz - Género (AI)")]
    public Toggle genderFToggle;
    public Toggle genderMToggle;

    private bool suppressUiEvents;

    // caches para restaurar volumes
    private float prevMusic = 1f;
    private float prevSfx = 1f;
    private float prevVoice = 1f;

    private void Start()
    {
        if (SoundColector.Instance == null)
        {
            Debug.LogWarning("[OptionsMenuUI] SoundColector.Instance é null. Verifica se o objeto SoundColector existe na primeira cena.");
            return;
        }

        InitAudioUI();
    }

    private void InitAudioUI()
    {
        suppressUiEvents = true;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.value = SoundColector.Instance.musicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.value = SoundColector.Instance.sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.onValueChanged.RemoveAllListeners();
            voiceVolumeSlider.value = SoundColector.Instance.voiceVolume;
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        }

        if (muteMusicToggle != null)
        {
            muteMusicToggle.onValueChanged.RemoveAllListeners();
            muteMusicToggle.isOn = (musicVolumeSlider != null ? musicVolumeSlider.value : SoundColector.Instance.musicVolume) <= 0f;
            muteMusicToggle.onValueChanged.AddListener(OnMuteMusicChanged);
        }

        if (muteSfxToggle != null)
        {
            muteSfxToggle.onValueChanged.RemoveAllListeners();
            muteSfxToggle.isOn = (sfxVolumeSlider != null ? sfxVolumeSlider.value : SoundColector.Instance.sfxVolume) <= 0f;
            muteSfxToggle.onValueChanged.AddListener(OnMuteSfxChanged);
        }

        if (muteVoiceToggle != null)
        {
            muteVoiceToggle.onValueChanged.RemoveAllListeners();
            muteVoiceToggle.isOn = (voiceVolumeSlider != null ? voiceVolumeSlider.value : SoundColector.Instance.voiceVolume) <= 0f;
            muteVoiceToggle.onValueChanged.AddListener(OnMuteVoiceChanged);
        }

        if (muteAllToggle != null)
        {
            muteAllToggle.onValueChanged.RemoveAllListeners();
            muteAllToggle.isOn = SoundColector.Instance.muteAll;
            muteAllToggle.onValueChanged.AddListener(OnMuteAllChanged);
        }

        // se estiver muteAll ligado, cache e força UI coerente
        if (muteAllToggle != null && muteAllToggle.isOn)
        {
            prevMusic = Mathf.Max(0.0001f, (musicVolumeSlider != null ? musicVolumeSlider.value : prevMusic));
            prevSfx = Mathf.Max(0.0001f, (sfxVolumeSlider != null ? sfxVolumeSlider.value : prevSfx));
            prevVoice = Mathf.Max(0.0001f, (voiceVolumeSlider != null ? voiceVolumeSlider.value : prevVoice));

            if (musicVolumeSlider != null) musicVolumeSlider.value = 0f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0f;
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = 0f;

            if (muteMusicToggle != null) muteMusicToggle.isOn = true;
            if (muteSfxToggle != null) muteSfxToggle.isOn = true;
            if (muteVoiceToggle != null) muteVoiceToggle.isOn = true;
        }

        InitVoiceToggles();
        suppressUiEvents = false;
    }

    private void InitVoiceToggles()
    {
        suppressUiEvents = true;

        // --- Configurar Dropdown de Idioma ---
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.ClearOptions();

            // Añadir las opciones (El orden importa: 0 = Inglés, 1 = Portugués)
            languageDropdown.options.Add(new TMP_Dropdown.OptionData("English"));
            languageDropdown.options.Add(new TMP_Dropdown.OptionData("Portuguese"));

            // Estado inicial a partir do SoundColector
            var lang = SoundColector.Instance.voiceLanguage;
            if (lang == SoundColector.VoiceLanguage.English)
                languageDropdown.value = 0;
            else
                languageDropdown.value = 1; // Portugués por defecto si no es inglés

            languageDropdown.RefreshShownValue();
            languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
        }

        // --- Configurar Toggles de Género ---
        if (genderFToggle != null) { genderFToggle.onValueChanged.RemoveAllListeners(); genderFToggle.onValueChanged.AddListener(OnGenderFChanged); }
        if (genderMToggle != null) { genderMToggle.onValueChanged.RemoveAllListeners(); genderMToggle.onValueChanged.AddListener(OnGenderMChanged); }

        var g = SoundColector.Instance.aiVoiceGender;

        if (genderFToggle != null) genderFToggle.isOn = (g == SoundColector.VoiceGender.Female);
        if (genderMToggle != null) genderMToggle.isOn = (g == SoundColector.VoiceGender.Male);

        // Garantizar 1 selecionado (género)
        if (!AnyOn(genderFToggle, genderMToggle))
        {
            if (genderMToggle != null) genderMToggle.isOn = true;
            else if (genderFToggle != null) genderFToggle.isOn = true;
        }

        suppressUiEvents = false;
    }

    private bool AnyOn(params Toggle[] toggles)
    {
        for (int i = 0; i < toggles.Length; i++)
            if (toggles[i] != null && toggles[i].isOn) return true;
        return false;
    }

    // --- LÓGICA DEL NUEVO DROPDOWN ---
    private void OnLanguageDropdownChanged(int index)
    {
        if (suppressUiEvents || SoundColector.Instance == null) return;

        // Según el índice seleccionado, asignamos el idioma (0 = English, 1 = Portugués)
        if (index == 0)
        {
            SoundColector.Instance.SetVoiceLanguage(SoundColector.VoiceLanguage.English);
        }
        else
        {
            SoundColector.Instance.SetVoiceLanguage(SoundColector.VoiceLanguage.Portuguese);
        }
    }

    // --- LÓGICA DE GÉNERO ---
    private void SelectGender(SoundColector.VoiceGender gender)
    {
        if (SoundColector.Instance == null) return;

        suppressUiEvents = true;
        if (genderFToggle != null) genderFToggle.isOn = (gender == SoundColector.VoiceGender.Female);
        if (genderMToggle != null) genderMToggle.isOn = (gender == SoundColector.VoiceGender.Male);
        suppressUiEvents = false;

        SoundColector.Instance.SetAIVoiceGender(gender);
    }

    private void EnsureGenderSelected()
    {
        if (AnyOn(genderFToggle, genderMToggle)) return;

        suppressUiEvents = true;
        if (genderMToggle != null) genderMToggle.isOn = true;
        else if (genderFToggle != null) genderFToggle.isOn = true;
        suppressUiEvents = false;
    }

    private void OnGenderFChanged(bool isOn)
    {
        if (suppressUiEvents || SoundColector.Instance == null) return;
        if (!isOn) { EnsureGenderSelected(); return; }
        SelectGender(SoundColector.VoiceGender.Female);
    }

    private void OnGenderMChanged(bool isOn)
    {
        if (suppressUiEvents || SoundColector.Instance == null) return;
        if (!isOn) { EnsureGenderSelected(); return; }
        SelectGender(SoundColector.VoiceGender.Male);
    }

    // --- LÓGICA DE VOLUMEN (Sin cambios) ---
    private void OnMusicVolumeChanged(float value)
    {
        if (suppressUiEvents) return;
        if (SoundColector.Instance == null) return;

        SoundColector.Instance.SetMusicVolume01(value);

        if (muteMusicToggle != null)
        {
            suppressUiEvents = true;
            muteMusicToggle.isOn = (value <= 0f);
            suppressUiEvents = false;
        }

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (suppressUiEvents) return;
        if (SoundColector.Instance == null) return;

        SoundColector.Instance.SetSfxVolume01(value);

        if (muteSfxToggle != null)
        {
            suppressUiEvents = true;
            muteSfxToggle.isOn = (value <= 0f);
            suppressUiEvents = false;
        }

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnVoiceVolumeChanged(float value)
    {
        if (suppressUiEvents) return;
        if (SoundColector.Instance == null) return;

        SoundColector.Instance.SetVoiceVolume01(value);

        if (muteVoiceToggle != null)
        {
            suppressUiEvents = true;
            muteVoiceToggle.isOn = (value <= 0f);
            suppressUiEvents = false;
        }

        if (muteAllToggle != null && value > 0f && muteAllToggle.isOn)
            muteAllToggle.isOn = false;
    }

    private void OnMuteMusicChanged(bool muted)
    {
        if (SoundColector.Instance == null || suppressUiEvents) return;

        suppressUiEvents = true;

        if (muted)
        {
            if (musicVolumeSlider != null)
                prevMusic = Mathf.Max(0.0001f, musicVolumeSlider.value);

            if (musicVolumeSlider != null) musicVolumeSlider.value = 0f;
            SoundColector.Instance.SetMusicVolume01(0f);
        }
        else
        {
            float v = Mathf.Clamp01(prevMusic);
            if (musicVolumeSlider != null) musicVolumeSlider.value = v;
            SoundColector.Instance.SetMusicVolume01(v);

            if (muteAllToggle != null && muteAllToggle.isOn) muteAllToggle.isOn = false;
        }

        suppressUiEvents = false;
    }

    private void OnMuteSfxChanged(bool muted)
    {
        if (SoundColector.Instance == null || suppressUiEvents) return;

        suppressUiEvents = true;

        if (muted)
        {
            if (sfxVolumeSlider != null)
                prevSfx = Mathf.Max(0.0001f, sfxVolumeSlider.value);

            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0f;
            SoundColector.Instance.SetSfxVolume01(0f);
        }
        else
        {
            float v = Mathf.Clamp01(prevSfx);
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = v;
            SoundColector.Instance.SetSfxVolume01(v);

            if (muteAllToggle != null && muteAllToggle.isOn) muteAllToggle.isOn = false;
        }

        suppressUiEvents = false;
    }

    private void OnMuteVoiceChanged(bool muted)
    {
        if (SoundColector.Instance == null || suppressUiEvents) return;

        suppressUiEvents = true;

        if (muted)
        {
            if (voiceVolumeSlider != null)
                prevVoice = Mathf.Max(0.0001f, voiceVolumeSlider.value);

            if (voiceVolumeSlider != null) voiceVolumeSlider.value = 0f;
            SoundColector.Instance.SetVoiceVolume01(0f);
        }
        else
        {
            float v = Mathf.Clamp01(prevVoice);
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = v;
            SoundColector.Instance.SetVoiceVolume01(v);

            if (muteAllToggle != null && muteAllToggle.isOn) muteAllToggle.isOn = false;
        }

        suppressUiEvents = false;
    }

    public void OnMuteAllChanged(bool muted)
    {
        if (SoundColector.Instance == null) return;

        suppressUiEvents = true;

        if (muted)
        {
            if (musicVolumeSlider != null) prevMusic = Mathf.Max(0.0001f, musicVolumeSlider.value);
            if (sfxVolumeSlider != null) prevSfx = Mathf.Max(0.0001f, sfxVolumeSlider.value);
            if (voiceVolumeSlider != null) prevVoice = Mathf.Max(0.0001f, voiceVolumeSlider.value);

            if (musicVolumeSlider != null) musicVolumeSlider.value = 0f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0f;
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = 0f;

            if (muteMusicToggle != null) muteMusicToggle.isOn = true;
            if (muteSfxToggle != null) muteSfxToggle.isOn = true;
            if (muteVoiceToggle != null) muteVoiceToggle.isOn = true;

            SoundColector.Instance.SetMuteAll(true);
        }
        else
        {
            if (musicVolumeSlider != null) musicVolumeSlider.value = prevMusic;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = prevSfx;
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = prevVoice;

            if (muteMusicToggle != null) muteMusicToggle.isOn = (prevMusic <= 0f);
            if (muteSfxToggle != null) muteSfxToggle.isOn = (prevSfx <= 0f);
            if (muteVoiceToggle != null) muteVoiceToggle.isOn = (prevVoice <= 0f);

            SoundColector.Instance.SetMuteAll(false);

            SoundColector.Instance.SetMusicVolume01(prevMusic);
            SoundColector.Instance.SetSfxVolume01(prevSfx);
            SoundColector.Instance.SetVoiceVolume01(prevVoice);
        }

        suppressUiEvents = false;
    }
}