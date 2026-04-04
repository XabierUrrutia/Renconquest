using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ponlo en el botón de "Reiniciar Progreso" de tu menú.
/// Borra todo el progreso guardado y vuelve al nivel 1 desbloqueado.
/// </summary>
[RequireComponent(typeof(Button))]
public class ResetProgressButton : MonoBehaviour
{
    [Tooltip("(Opcional) Panel de confirmación antes de borrar. Si no tienes, déjalo vacío.")]
    public GameObject confirmationPanel;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        SoundColector.Instance?.PlayUiClick();

        if (confirmationPanel != null)
        {
            // Mostrar panel de confirmación primero
            confirmationPanel.SetActive(true);
        }
        else
        {
            // Sin confirmación: borrar directamente
            ResetProgress();
        }
    }

    // Llama a este método desde el botón "Sí" de tu panel de confirmación
    public void ConfirmReset()
    {
        SoundColector.Instance?.PlayUiClick();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        ResetProgress();
    }

    // Llama a este método desde el botón "No/Cancelar" de tu panel de confirmación
    public void CancelReset()
    {
        SoundColector.Instance?.PlayUiClick();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("UnlockedLevel", 1);
        PlayerPrefs.Save();

        Debug.Log("[ResetProgressButton] Progreso reiniciado.");

        // Refrescar todos los LevelButtons que haya en la escena
        foreach (var btn in FindObjectsOfType<LevelButton>())
            btn.Refresh();
    }
}