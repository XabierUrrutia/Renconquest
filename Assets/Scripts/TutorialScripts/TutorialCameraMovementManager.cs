using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Tutorial em 4 passos para mover a câmera: cima, baixo, esquerda, direita (WASD).
/// Avança automaticamente quando o jogador move a câmera na direção correta.
/// Após os 4 passos dá um período livre (freeMoveDuration) para o jogador mexer livremente,
/// depois do período o tutorial pausa o jogo e abre o painel de opções para voltar ao menu.
/// Mostra um indicador visual (seta) apontando a direção pedida.
/// </summary>
public class TutorialCameraMovementManager : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right }

    [Header("Referências UI / Narrador")]
    public TutorialNarrator narrator;                 // opcional: mostra as falas configuradas
    [Tooltip("Índices das falas no TutorialNarrator para cada passo (0-based). Tamanho = 4")]
    public int[] narratorLineIndex = new int[4] { 0, 1, 2, 3 };

    public GameObject stepCompletePanel;              // painel final com contador (opcional)
    public TextMeshProUGUI stepMessageText;           // texto do painel
    public string nextSceneName = "";                 // opcional: cena seguinte a carregar ao fim

    [Header("Configuração dos passos")]
    public Direction[] steps = new Direction[4] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
    public float requiredMoveDistance = 0.5f;         // distância mínima de deslocamento da câmera para validar
    public float freeMoveDuration = 10f;              // tempo livre após completar os 4 passos

    [Header("Indicador visual (seta)")]
    [Tooltip("Prefab da seta (World space). Será instanciado e posicionado próximo ao centro da câmera.")]
    public GameObject arrowPrefab;
    [Tooltip("Distância da seta a partir da posição da câmera (unidades mundo)")]
    public float arrowDistance = 3f;
    [Tooltip("Offset vertical (Z) para garantir que a seta aparece acima do mapa")]
    public float arrowZ = -1f;

    [Header("Eventos")]
    public UnityEvent onTutorialComplete;

    int currentStep = 0;
    Camera cam;
    Vector3 stepStartPos;
    bool waitingForMove = false;
    bool tutorialFinished = false;

    // runtime arrow
    private GameObject arrowInstance;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
            Debug.LogWarning("TutorialCameraMovementManager: Camera.main não encontrada.");

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        currentStep = 0;
        StartStep(currentStep);
    }

    void Update()
    {
        if (tutorialFinished) return;
        if (!waitingForMove) return;

        // Detecta input direto (WASD) OU deslocamento da câmera desde o início do passo
        bool moved = false;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;

        switch (steps[currentStep])
        {
            case Direction.Up:
                if (Input.GetKeyDown(KeyCode.W)) moved = true;
                if (camPos.y - stepStartPos.y >= requiredMoveDistance) moved = true;
                break;
            case Direction.Down:
                if (Input.GetKeyDown(KeyCode.S)) moved = true;
                if (stepStartPos.y - camPos.y >= requiredMoveDistance) moved = true;
                break;
            case Direction.Left:
                if (Input.GetKeyDown(KeyCode.A)) moved = true;
                if (stepStartPos.x - camPos.x >= requiredMoveDistance) moved = true;
                break;
            case Direction.Right:
                if (Input.GetKeyDown(KeyCode.D)) moved = true;
                if (camPos.x - stepStartPos.x >= requiredMoveDistance) moved = true;
                break;
        }

        if (moved)
        {
            CompleteCurrentStep();
        }
    }

    void StartStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= steps.Length)
        {
            EndTutorial();
            return;
        }

        // Guarda posição inicial da câmera e habilita escuta
        stepStartPos = cam != null ? cam.transform.position : Vector3.zero;
        waitingForMove = true;

        // Mostrar fala do narrador se atribuído
        if (narrator != null && narratorLineIndex != null && stepIndex < narratorLineIndex.Length)
        {
            int line = narratorLineIndex[stepIndex];
            narrator.ShowLineIndex(line);
        }

        // Mostrar mensagem simples no HUD (opcional)
        if (stepMessageText != null)
        {
            stepMessageText.text = $"Passo {stepIndex + 1}/{steps.Length}: mover a câmera para {steps[stepIndex]} (WASD)";
        }

        // Mostra seta indicadora
        ShowArrowForDirection(steps[stepIndex]);

        Debug.Log($"TutorialCamera: iniciou passo {stepIndex} -> {steps[stepIndex]}");
    }

    void CompleteCurrentStep()
    {
        waitingForMove = false;
        HideArrow();
        Debug.Log($"TutorialCamera: passo {currentStep} concluído ({steps[currentStep]})");

        // Avança
        currentStep++;

        if (currentStep >= steps.Length)
        {
            // passo final: permitir movimento livre por X segundos e depois terminar
            StartCoroutine(FreeMoveThenPauseAndOpenOptions());
        }
        else
        {
            // inicia próximo passo automaticamente
            StartStep(currentStep);
        }
    }

    IEnumerator FreeMoveThenPauseAndOpenOptions()
    {
        // Mensagem e painel durante o tempo livre
        if (stepMessageText != null)
            stepMessageText.text = $"Tutorial completo! Tens {freeMoveDuration} segundos de movimento livre.";

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(true);

        float t = 0f;
        while (t < freeMoveDuration)
        {
            t += Time.deltaTime;
            if (stepMessageText != null)
                stepMessageText.text = $"Movimento livre: {Mathf.CeilToInt(freeMoveDuration - t)}s";
            yield return null;
        }

        // Fecha o painel de tempo
        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        // Pausa o jogo e abre o painel de opções (PauseMenu)
        var pauseMenu = FindObjectOfType<PauseMenu>();
        if (pauseMenu != null)
        {
            pauseMenu.PauseGame();
            Debug.Log("TutorialCamera: tempo livre terminado — PauseMenu aberto.");
        }
        else
        {
            // Fallback: pausa manualmente e mostra painel (se existente)
            Time.timeScale = 0f;
            Debug.LogWarning("TutorialCamera: PauseMenu não encontrado — jogo pausado manualmente.");
            if (stepCompletePanel != null)
                stepCompletePanel.SetActive(true);
        }

        // Notifica final (opcional)
        onTutorialComplete?.Invoke();
    }

    void EndTutorial()
    {
        tutorialFinished = true;

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        onTutorialComplete?.Invoke();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.Log("TutorialCamera: terminado.");
    }

    // Arrow helper
    void ShowArrowForDirection(Direction dir)
    {
        HideArrow();

        if (arrowPrefab == null || cam == null) return;

        // calcula posição para a seta: um pouco à frente da câmera na direção pedida
        Vector3 dirVec = DirectionToVector(dir);
        Vector3 pos = cam.transform.position + (Vector3)(dirVec.normalized * arrowDistance);
        pos.z = arrowZ;

        arrowInstance = Instantiate(arrowPrefab, pos, Quaternion.identity);
        if (arrowInstance != null)
        {
            arrowInstance.transform.up = new Vector3(dirVec.x, dirVec.y, 0f);
            arrowInstance.transform.SetParent(cam.transform, true);
        }
    }

    void HideArrow()
    {
        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
            arrowInstance = null;
        }
    }

    Vector2 DirectionToVector(Direction d)
    {
        switch (d)
        {
            case Direction.Up: return Vector2.up;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
            case Direction.Right: return Vector2.right;
            default: return Vector2.zero;
        }
    }

    // Métodos utilitários para debug / inspector
    public void SimulateCompleteCurrentStep()
    {
        if (!tutorialFinished && waitingForMove) CompleteCurrentStep();
    }

    public void RestartTutorial()
    {
        StopAllCoroutines();
        tutorialFinished = false;
        currentStep = 0;
        StartStep(currentStep);
    }
}