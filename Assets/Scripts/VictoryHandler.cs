using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Ponlo en el mismo GameObject que EnemyBase.
/// Se encarga de registrar la victoria en el LevelManager cuando la base es conquistada.
/// </summary>
public class VictoryHandler : MonoBehaviour
{
    [Header("Botones del Canvas de Victoria")]
    [Tooltip("Botón para continuar al siguiente nivel")]
    public Button nextLevelButton;

    [Tooltip("Botón para volver al menú principal")]
    public Button mainMenuButton;

    [Tooltip("Nombre de la escena de selección de mapas")]
    public string mainMenuSceneName = "Portugal";

    private EnemyBase enemyBase;
    private bool victoryRegistered = false;

    void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    void Start()
    {
        // Enlazar botones si están asignados
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    void Update()
    {
        // Detectamos la victoria en cuanto EnemyBase la marca
        if (!victoryRegistered && enemyBase != null && enemyBase.isConquered)
        {
            RegisterVictory();
        }
    }

    private void RegisterVictory()
    {
        victoryRegistered = true;

        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("[VictoryHandler] LevelManager.Instance es null. ¿Está en la escena?");
            return;
        }

        int level = LevelManager.Instance.CurrentLevel;

        if (level <= 0)
        {
            Debug.LogWarning("[VictoryHandler] La escena actual no está registrada en LevelManager.");
            return;
        }

        // Guardar que este nivel fue completado y desbloquear el siguiente
        LevelManager.Instance.MarkLevelCompleted(level);
        LevelManager.Instance.UnlockNextLevel(level);

        Debug.Log($"[VictoryHandler] Nivel {level} completado y guardado. Siguiente nivel desbloqueado.");
    }

    // --- Botones del Canvas de Victoria ---

    public void OnNextLevel()
    {
        SoundColector.Instance?.PlayUiClick();
        Time.timeScale = 1f;

        // Volver al menú principal para que el jugador elija el nivel desbloqueado
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnMainMenu()
    {
        SoundColector.Instance?.PlayUiClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}