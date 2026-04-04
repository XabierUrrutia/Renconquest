using UnityEngine;
using UnityEngine.SceneManagement;

public class WinPanel : MonoBehaviour
{
    [Tooltip("Panel UI que muestra WIN (inactive por default)")]
    public GameObject winPanelUI;

    [Tooltip("Nombre de la escena de selección de niveles")]
    public string levelSelectScene = "Portugal";

    private bool _shown = false;

    void Start()
    {
        if (winPanelUI != null)
            winPanelUI.SetActive(false);
    }

    public void ShowWin()
    {
        if (_shown) return;
        _shown = true;

        SoundColector.Instance?.PlayUiPanelOpen();
        SoundColector.Instance?.PlayVictoryMusic();

        if (winPanelUI != null)
            winPanelUI.SetActive(true);

        Time.timeScale = 0f;

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("[WinPanel] LevelManager no encontrado.");
            return;
        }

        int lvl = LevelManager.Instance.CurrentLevel;
        if (lvl <= 0)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            lvl = LevelManager.Instance.GetLevelIndexByScene(sceneName);
        }

        if (lvl > 0)
        {
            LevelManager.Instance.MarkLevelCompleted(lvl);
            LevelManager.Instance.UnlockNextLevel(lvl);
            Debug.Log($"[WinPanel] Nivel {lvl} completado. Siguiente desbloqueado.");
        }
    }

    public void HideWin()
    {
        if (!_shown) return;
        _shown = false;

        if (winPanelUI != null)
            winPanelUI.SetActive(false);

        Time.timeScale = 1f;
    }

    // Botón MAP -> volver a selección de niveles
    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;
        SoundColector.Instance?.PlayUiClick();
        SceneManager.LoadScene(2); // Portugal es índice 2
    }
}