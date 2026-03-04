using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VideoSettingsUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;

    void Start()
    {
        // Configuración inicial del Toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // Obtener y filtrar resoluciones (Igual que antes)
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            bool isDuplicate = false;
            for (int j = 0; j < filteredResolutions.Count; j++)
            {
                if (filteredResolutions[j].width == resolutions[i].width &&
                    filteredResolutions[j].height == resolutions[i].height)
                {
                    isDuplicate = true; break;
                }
            }

            if (!isDuplicate)
            {
                filteredResolutions.Add(resolutions[i]);
                options.Add(option);
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                    currentResolutionIndex = filteredResolutions.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        ApplySettings(resolutionIndex, fullscreenToggle.isOn);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        ApplySettings(resolutionDropdown.value, isFullscreen);
    }

    // --- AQUÍ ESTÁ EL CAMBIO CLAVE ---
    private void ApplySettings(int resolutionIndex, bool isFullscreen)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];

        if (isFullscreen)
        {
            // Opción 1: Pantalla completa exclusiva (Mejor rendimiento)
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;

            // Opción 2: Pantalla completa sin bordes (Más rápido al alt-tab)
            // Screen.fullScreenMode = FullScreenMode.FullScreenWindow; 

            Screen.SetResolution(resolution.width, resolution.height, true);
        }
        else
        {
            // FORZAMOS MODO VENTANA
            Screen.fullScreenMode = FullScreenMode.Windowed;

            // IMPORTANTE: Aplicamos la resolución en modo ventana
            Screen.SetResolution(resolution.width, resolution.height, false);
        }

        Debug.Log($"Res: {resolution.width}x{resolution.height} | Mode: {Screen.fullScreenMode}");
    }
}