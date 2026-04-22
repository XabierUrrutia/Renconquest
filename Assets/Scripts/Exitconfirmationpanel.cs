using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ponlo en el ConfirmationPanel.
/// Los botones de casa y Map llaman a MostrarConfirmacion() en vez de navegar directamente.
/// </summary>
public class ExitConfirmationPanel : MonoBehaviour
{
    [Tooltip("Nombre de la escena a la que ir al confirmar (MainMenu o Portugal)")]
    private string escenaDestino = "";
    private bool eraTimescaleCero = false;

    void Start()
    {
        gameObject.SetActive(false);
    }

    // Llama a este método desde el botón de la casa
    public void MostrarConfirmacionMainMenu()
    {
        escenaDestino = "MainMenu";
        MostrarPanel();
    }

    // Llama a este método desde el botón de Map
    public void MostrarConfirmacionMapa()
    {
        escenaDestino = "Portugal";
        MostrarPanel();
    }

    void MostrarPanel()
    {
        SoundColector.Instance?.PlayUiClick();

        // Pausar el juego mientras se muestra el panel
        eraTimescaleCero = Time.timeScale == 0f;
        Time.timeScale = 0f;

        gameObject.SetActive(true);
    }

    // Botón "Sí" 
    public void Confirmar()
    {
        SoundColector.Instance?.PlayUiClick();
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        SceneManager.LoadScene(escenaDestino);
    }

    // Botón "No"
    public void Cancelar()
    {
        SoundColector.Instance?.PlayUiClick();
        gameObject.SetActive(false);

        // Restaurar timeScale solo si no estaba pausado antes
        if (!eraTimescaleCero)
            Time.timeScale = 1f;
    }
}