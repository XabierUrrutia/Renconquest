using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialStep1Manager : MonoBehaviour
{
    [Header("UI")]
    public GameObject stepCompletePanel;          // Painel que aparece quando passar o passo
    public Button proceedButton;                  // Botão para ir à próxima cena
    public TextMeshProUGUI stepMessageText;       // Mensagem que mostra "Passaste o passo X"
    public string nextSceneName = "";             // Nome da cena do passo 2

    [Header("Trigger auto-bind")]
    public int triggerStepIndex = 0;              // Índice do trigger que corresponde a este passo (normalmente 0)

    void Start()
    {
        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        // Tenta ligar automaticamente a qualquer TutorialTrigger com o índice correto
        var triggers = FindObjectsOfType<TutorialTrigger>();
        foreach (var t in triggers)
        {
            if (t.stepIndex == triggerStepIndex)
            {
                // adiciona listener para quando o trigger for ativado
                t.onPlayerEnter.AddListener(OnPlayerReachedTower);
            }
        }
    }

    // Método público para conectar manualmente ao TutorialTrigger.onPlayerEnter (opção)
    public void OnPlayerReachedTower()
    {
        ShowStepCompleteUI();
    }

    void ShowStepCompleteUI()
    {
        if (stepMessageText != null)
            stepMessageText.text = "You've finished the first tutorial level. Now go to the next level:";

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(true);

        // 🔹 NOVO: esconder o painel do narrador, se ainda estiver ativo
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

        // Cancela verificações pendentes de GameOver antes de mudar de cena
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
            // garantir que o tempo está normalizado caso o tutorial tenha pausado o jogo
            Time.timeScale = 1f;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // Utilitário para testes manuais via Inspector
    public void SimulateComplete()
    {
        OnPlayerReachedTower();
    }
}