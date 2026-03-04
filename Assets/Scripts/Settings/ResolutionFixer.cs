using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResolutionFixer : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Canvas mainCanvas; // ARRASTRA TU CANVAS AQUÍ EN EL INSPECTOR

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;

    void Start()
    {
        // 1. Setup inicial (igual que antes pero simplificado)
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            // Filtro básico
            if (!options.Contains(option))
            {
                filteredResolutions.Add(resolutions[i]);
                options.Add(option);
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                    currentResIndex = options.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(delegate { ChangeResolution(); });
        }

        resolutionDropdown.onValueChanged.AddListener(delegate { ChangeResolution(); });
    }

    // Esta función llama a la Corrutina
    public void ChangeResolution()
    {
        StartCoroutine(ApplyResolutionDelayed());
    }

    IEnumerator ApplyResolutionDelayed()
    {
        // 1. Obtenemos valores
        int index = resolutionDropdown.value;
        bool fullScreen = fullscreenToggle.isOn;
        int width = filteredResolutions[index].width;
        int height = filteredResolutions[index].height;

        Debug.Log($"Aplicando: {width}x{height} en {(fullScreen ? "Fullscreen" : "Ventana")}");

        // 2. Cambiamos el modo ANTES de la resolución
        if (fullScreen)
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        else
            Screen.fullScreenMode = FullScreenMode.Windowed;

        // 3. Aplicamos la resolución
        Screen.SetResolution(width, height, fullScreen);

        // 4. EL TRUCO: Esperar a que Windows termine de redimensionar la ventana
        // Esperamos 2 frames o un poco de tiempo real
        yield return new WaitForSeconds(0.2f);

        // 5. Forzamos al Canvas a "despertar" y recalcular los botones
        if (mainCanvas != null)
        {
            mainCanvas.enabled = false;
            // Esperamos un frame apagado
            yield return null;
            mainCanvas.enabled = true;

            // Forzamos actualización de layouts
            Canvas.ForceUpdateCanvases();
        }
    }
}