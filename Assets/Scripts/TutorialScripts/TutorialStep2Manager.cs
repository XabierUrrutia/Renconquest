using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gerencia o passo 2 do tutorial: subscreve mortes de inimigos e garante que os spawners
/// da cena iniciem para que os inimigos apareçam durante o tutorial.
/// </summary>
public class TutorialStep2Manager : MonoBehaviour
{
    [Header("UI")]
    public GameObject stepCompletePanel;
    public Button proceedButton;
    public TextMeshProUGUI stepMessageText;
    public string nextSceneName = "";

    [Header("Enemy subscription")]
    public string enemyTag = "Enemy";
    public GameObject specificEnemy;

    void Start()
    {
        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        Debug.Log("[TutorialStep2Manager] Start() a procurar EnemyDeathListener(s) na cena...");
        if (specificEnemy != null)
        {
            var listener = specificEnemy.GetComponent<EnemyDeathListener>();
            if (listener != null)
            {
                listener.onEnemyDied.AddListener(OnEnemyDeath);
                Debug.Log($"[TutorialStep2Manager] ligado ao specificEnemy '{specificEnemy.name}'");
            }
            else
                Debug.LogWarning($"TutorialStep2Manager: specificEnemy '{specificEnemy.name}' não tem EnemyDeathListener.");
        }
        else
        {
            EnemyDeathListener[] listeners = FindObjectsOfType<EnemyDeathListener>();
            foreach (var l in listeners)
            {
                if (string.IsNullOrEmpty(enemyTag) || l.CompareTag(enemyTag) || (l.gameObject != null && l.gameObject.tag == enemyTag))
                {
                    l.onEnemyDied.AddListener(OnEnemyDeath);
                    Debug.Log($"[TutorialStep2Manager] Subscrito a onEnemyDied de '{l.gameObject.name}'");
                }
            }

            if (listeners.Length == 0)
                Debug.LogWarning("TutorialStep2Manager: nenhum EnemyDeathListener encontrado na cena.");
        }

        // Garantir que os spawners da cena iniciem (evita dependência de proximidade / tagging no tutorial)
        StartSpawnersInScene();
    }

    // Inicia spawners encontrados na cena — útil para cenas de tutorial onde queremos spawn imediato
    void StartSpawnersInScene()
    {
        var spawners = FindObjectsOfType<EnemySpawner>();
        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("[TutorialStep2Manager] Nenhum EnemySpawner encontrado na cena.");
            return;
        }

        foreach (var s in spawners)
        {
            if (s == null) continue;

            // Forçar ativação do spawn mesmo que o spawner esteja configurado para esperar proximidade
            // chama StartSpawning() que já respeita validações internas (prefab etc).
            s.StartSpawning();
            Debug.Log($"[TutorialStep2Manager] Forçando StartSpawning() em spawner '{s.name}'");
        }
    }

    public void OnEnemyDeath()
    {
        Debug.Log("[TutorialStep2Manager] OnEnemyDeath recebido");
        ShowStepCompleteUI();
    }

    void ShowStepCompleteUI()
    {
        if (stepMessageText != null)
            stepMessageText.text = "You've finished the first tutorial level. Now go to the next level:";

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(true);

        // esconder painel do narrador, se ativo
        TutorialNarrator narrator = FindObjectOfType<TutorialNarrator>();
        if (narrator != null && narrator.panelToClose != null && narrator.panelToClose.activeSelf)
        {
            narrator.panelToClose.SetActive(false);
            Debug.Log("Painel do narrador escondido ao mostrar o painel de sucesso.");
        }

        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveAllListeners();
            proceedButton.onClick.AddListener(ProceedToNextScene);
            proceedButton.interactable = true;
        }
    }
    void ProceedToNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("TutorialStep1Manager: nextSceneName não definido.");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ResetGame();

        SceneManager.LoadScene(nextSceneName);
    }
    // Método utilitário para testar no Inspector
    public void SimulateEnemyDeath()
    {
        Debug.Log("[TutorialStep2Manager] SimulateEnemyDeath() chamado manualmente.");
        OnEnemyDeath();
    }
}
