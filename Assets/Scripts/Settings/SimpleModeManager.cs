using UnityEngine;
using TMPro;
using UnityEngine.UI; // Necesario para tocar el Canvas Scaler
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class SimpleModeManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown displayDropdown;
    public Canvas mainCanvas;

    // --- DLLs para Minimizar ---
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    const int SW_MINIMIZE = 6;

    void Start()
    {
        // 1. FORZAR CONFIGURACIÓN DEL CANVAS (El arreglo que necesitas)
        ConfigurarCanvas();

        // 2. Configurar Dropdown
        displayDropdown.ClearOptions();
        displayDropdown.options.Add(new TMP_Dropdown.OptionData("Pantalla Completa"));
        displayDropdown.options.Add(new TMP_Dropdown.OptionData("Modo Ventana"));
        displayDropdown.options.Add(new TMP_Dropdown.OptionData("Minimizar"));

        displayDropdown.value = Screen.fullScreen ? 0 : 1;
        displayDropdown.RefreshShownValue();
        displayDropdown.onValueChanged.AddListener(OnModeChanged);
    }

    // --- ESTA ES LA FUNCIÓN NUEVA QUE ARREGLA EL ESCALADO ---
    void ConfigurarCanvas()
    {
        if (mainCanvas != null)
        {
            CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); // Tu resolución base
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f; // Equilibrio perfecto
            }
        }
    }

    public void OnModeChanged(int index)
    {
        StartCoroutine(ChangeModeRoutine(index));
    }

    IEnumerator ChangeModeRoutine(int index)
    {
        if (mainCanvas != null) mainCanvas.enabled = false;

        switch (index)
        {
            case 0: // Fullscreen
                Resolution maxRes = Screen.resolutions[Screen.resolutions.Length - 1];
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Screen.SetResolution(maxRes.width, maxRes.height, true);
                break;

            case 1: // Ventana
                Screen.fullScreenMode = FullScreenMode.Windowed;
                // Ponemos 1280x720 (HD) que es un tamaño seguro para ventana
                Screen.SetResolution(1280, 720, false);
                break;

            case 2: // Minimizar
                MinimizeGame();
                displayDropdown.SetValueWithoutNotify(Screen.fullScreen ? 0 : 1);
                break;
        }

        yield return new WaitForSeconds(0.2f); // Esperar a Windows

        if (mainCanvas != null) mainCanvas.enabled = true; // Reactivar UI
    }

    private void MinimizeGame()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr handle = GetActiveWindow();
            ShowWindow(handle, SW_MINIMIZE);
#endif
    }
}