using UnityEngine;
using UnityEngine.SceneManagement;

public class WinPanel : MonoBehaviour
{
    [Tooltip("Panel UI que mostra WIN (inactive por default)")]
    public GameObject winPanelUI;

    private bool _shown = false;

    void Start()
    {
        if (winPanelUI != null)
            winPanelUI.SetActive(false);
    }

    public void ShowWin()
    {
        SoundColector.Instance?.PlayUiPanelOpen();
        SoundColector.Instance?.PlayVictoryMusic();


        if (_shown) return;
        _shown = true;

        if (winPanelUI != null)
            winPanelUI.SetActive(true);

        Time.timeScale = 0f;

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("[WinPanel] LevelManager não encontrado. Não foi possível marcar/desbloquear níveis.");
            return;
        }

        // Tentar obter o nível atual de forma robusta:
        int lvl = LevelManager.Instance.CurrentLevel;
        if (lvl <= 0)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            lvl = LevelManager.Instance.GetLevelIndexByScene(sceneName);
            Debug.Log($"[WinPanel] CurrentLevel indefinido. Determinado por cena: '{sceneName}' -> nível {lvl}");
        }

        if (lvl > 0)
        {
            LevelManager.Instance.MarkLevelCompleted(lvl);
            LevelManager.Instance.UnlockNextLevel(lvl);
            Debug.Log($"[WinPanel] Nível {lvl} concluído. Próximo desbloqueado (se houver).");
        }
        else
        {
            Debug.LogWarning("[WinPanel] Não foi possível determinar o nível atual para marcar/desbloquear.");
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
}