using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Interface_Buttons : MonoBehaviour
{
    private static Stack<int> sceneHistory = new Stack<int>();

    // VARIABLES ESTÁTICAS PARA CONTROLAR EL MENÚ FLOTANTE
    private static bool optionsOpen = false;
    private static int optionsSceneIndex = 11; // Por defecto la 11, pero cambiará dinámicamente
    private static int gameSceneBeforeOptions = -1;

    void Start()
    {
        // SEGURIDAD: Si iniciamos y solo hay 1 escena cargada, forzamos el reset.
        // Esto arregla bloqueos si paraste el juego a medias en el editor.
        if (SceneManager.sceneCount == 1)
        {
            optionsOpen = false;
            Time.timeScale = 1f;
        }
    }

    // ------------------------------------------------------------------------
    // SISTEMA DE CARGA ADITIVA (ABRIR ENCIMA)
    // ------------------------------------------------------------------------

    public void OpenAdditiveMenu(int sceneIndex)
    {
        SoundColector.Instance?.PlayUiClick();

        if (optionsOpen) return; // Si ya está abierto, no hacer nada

        // Guardar posición por seguridad
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) PlayerPositionManager.SavePosition(player.transform.position);

        gameSceneBeforeOptions = SceneManager.GetActiveScene().buildIndex;
        SoundColector.Instance?.PlayPauseMusic();

        StartCoroutine(LoadAdditiveRoutine(sceneIndex));
    }

    IEnumerator LoadAdditiveRoutine(int index)
    {
        optionsOpen = true; // Bloqueamos: "Hay un menú abierto"
        Time.timeScale = 0f; // Pausar juego

        var op = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        optionsSceneIndex = index; // Recordamos qué escena abrimos para poder cerrarla luego
    }

    // ------------------------------------------------------------------------
    // FUNCIONES DE LOS BOTONES
    // ------------------------------------------------------------------------

    public void GoToChooseSettings()
    {
        // Lógica inteligente: Menú -> Normal | Juego -> Aditivo
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            LoadSceneAndSave(1);
        }
        else
        {
            OpenAdditiveMenu(1);
        }
    }

    public void GotoSettingsMenu()
    {
        // Lógica inteligente: Menú -> Normal | Juego -> Aditivo
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            SoundColector.Instance?.PlayUiClick();
            LoadSceneAndSave(11);
        }
        else
        {
            OpenAdditiveMenu(11);
        }
    }

    public void GoBack()
    {
        SoundColector.Instance?.PlayUiClick();

        // CASO 1: Estamos en modo "Menú Flotante" (Jugando + Opciones abiertas)
        if (optionsOpen)
        {
            // --- CORRECCIÓN CRÍTICA ---
            // 1. Reactivamos el tiempo PRIMERO
            Time.timeScale = 1f;

            // 2. Marcamos como cerrado PRIMERO
            optionsOpen = false;

            // 3. Y AHORA descargamos la escena. 
            // Hacemos esto al final porque al descargar la escena, este script 
            // podría ser destruido si vive dentro del menú de opciones.
            SceneManager.UnloadSceneAsync(optionsSceneIndex);

            Debug.Log("Menú cerrado y variables reseteadas correctamente.");
            return;
        }

        // CASO 2: Navegación normal (Menú principal, historial de pantallas)
        if (sceneHistory.Count > 1)
        {
            sceneHistory.Pop();
            int previousSceneIndex = sceneHistory.Peek();
            SceneManager.LoadScene(previousSceneIndex);
        }
        else
        {
            // Si no hay historial, volver al menú principal por defecto
            SceneManager.LoadScene(0);
        }
    }

    // ------------------------------------------------------------------------
    // UTILIDADES
    // ------------------------------------------------------------------------

    private void LoadSceneAndSave(int sceneIndex)
    {
        // Guardar historial
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneHistory.Count == 0 || sceneHistory.Peek() != currentSceneIndex)
        {
            sceneHistory.Push(currentSceneIndex);
        }

        // Guardar posición
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) PlayerPositionManager.SavePosition(player.transform.position);

        SceneManager.LoadScene(sceneIndex);
    }

    // Funciones "puente" para tus botones antiguos
    public void GoToOptionsFromGame() => OpenAdditiveMenu(1);
    public void GoToMainMenu() { SoundColector.Instance?.PlayMenuMusic(); LoadSceneAndSave(0); }
    public void GotoGame() => LoadSceneAndSave(2);
    public void GoToMap() => LoadSceneAndSave(2);
    public void SecondLevel() => LoadSceneAndSave(3);
    public void GotoControlsMenu() => LoadSceneAndSave(10);
    public void GoInGameSettings() => LoadSceneAndSave(2);
    public void GoTutorial1() => LoadSceneAndSave(6);
    public void GoToLevel1()
    {
        if (MoneyManager.Instance != null) MoneyManager.Instance.ResetMoney();
        LoadSceneAndSave(4);
    }
    public void GoToLevel2() => LoadSceneAndSave(3);
    public void GoToLevel3() => LoadSceneAndSave(5);

    public void QuitGame()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }
}