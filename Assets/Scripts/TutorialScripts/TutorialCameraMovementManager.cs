using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Tutorial em 4 passos para mover a c�mera: cima, baixo, esquerda, direita (WASD).
/// Avan�a automaticamente quando o jogador move a c�mera na dire��o correta.
/// Ap�s os 4 passos d� um per�odo livre (freeMoveDuration) para o jogador mexer livremente,
/// depois do per�odo o tutorial pausa o jogo e abre o painel de op��es para voltar ao menu.
/// Mostra um indicador visual (seta) apontando a dire��o pedida.
/// </summary>
public class TutorialCameraMovementManager : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right, Zoom }

    [Header("Refer�ncias UI / Narrador")]
    public TutorialNarrator narrator;                 // opcional: mostra as falas configuradas
    [Tooltip("�ndices das falas no TutorialNarrator para cada passo (0-based). Tamanho = 5")]
    public int[] narratorLineIndex = new int[5] { 0, 1, 2, 3, 4 };

    public GameObject stepCompletePanel;              // painel final com contador (opcional)
    public TextMeshProUGUI stepMessageText;           // texto do painel
    public string nextSceneName = "";                 // opcional: cena seguinte a carregar ao fim

    [Header("Configura��o dos passos")]
    public Direction[] steps = new Direction[5] { Direction.Up, Direction.Down, Direction.Left, Direction.Right, Direction.Zoom };
    public float requiredMoveDistance = 0.5f;         // dist�ncia m�nima de deslocamento da c�mera para validar
    public float freeMoveDuration = 20f;              // tempo livre ap�s completar os 4 passos

    [Header("Indicador visual (seta)")]
    [Tooltip("Prefab da seta (World space). Ser� instanciado e posicionado pr�ximo ao centro da c�mera.")]
    public GameObject arrowPrefab;
    [Tooltip("Dist�ncia da seta a partir da posi��o da c�mera (unidades mundo)")]
    public float arrowDistance = 3f;
    [Tooltip("Offset vertical (Z) para garantir que a seta aparece acima do mapa")]
    public float arrowZ = -1f;

    [Header("Eventos")]
    public UnityEvent onTutorialComplete;

    int currentStep = 0;
    Camera cam;
    cameraFollow camFollow;
    Vector3 stepStartPos;
    float stepStartZoom;
    bool waitingForMove = false;
    bool tutorialFinished = false;

    // runtime arrow
    private GameObject arrowInstance;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
            Debug.LogWarning("TutorialCameraMovementManager: Camera.main n�o encontrada.");

        camFollow = FindObjectOfType<cameraFollow>();

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        currentStep = 0;
        StartStep(currentStep);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (tutorialFinished) return;
        if (!waitingForMove) return;

        // Detecta input direto (WASD) OU deslocamento da c�mera desde o in�cio do passo
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
            case Direction.Zoom:
                if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.0001f) moved = true;
                if (cam != null && Mathf.Abs(cam.orthographicSize - stepStartZoom) >= 0.5f) moved = true;
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

        // Guarda posi��o inicial da c�mera e habilita escuta
        stepStartPos = cam != null ? cam.transform.position : Vector3.zero;
        stepStartZoom = cam != null ? cam.orthographicSize : 0f;
        waitingForMove = true;

        // Bloqueia só o que não é necessário para este passo
        if (camFollow != null)
        {
            camFollow.blockMovement = steps[stepIndex] == Direction.Zoom;
            camFollow.blockZoom = steps[stepIndex] != Direction.Zoom;
        }

        // Mostrar fala do narrador se atribu�do
        if (narrator != null && narratorLineIndex != null && stepIndex < narratorLineIndex.Length)
        {
            int line = narratorLineIndex[stepIndex];
            narrator.ShowLineIndex(line);
        }

        // Mostrar mensagem simples no HUD (opcional)
        if (stepMessageText != null)
        {
            stepMessageText.text = steps[stepIndex] == Direction.Zoom
                ? $"Step {stepIndex + 1}/{steps.Length}: zoom the camera (Mouse Scroll Wheel)"
                : $"Step {stepIndex + 1}/{steps.Length}: move the camera {steps[stepIndex]} (WASD)";
        }

        // Mostra seta indicadora (n/a para zoom)
        if (steps[stepIndex] != Direction.Zoom)
            ShowArrowForDirection(steps[stepIndex]);

        Debug.Log($"TutorialCamera: iniciou passo {stepIndex} -> {steps[stepIndex]}");
    }

    void CompleteCurrentStep()
    {
        waitingForMove = false;
        HideArrow();
        Debug.Log($"TutorialCamera: passo {currentStep} conclu�do ({steps[currentStep]})");

        // Avan�a
        currentStep++;

        if (currentStep >= steps.Length)
        {
            // passo final: permitir movimento livre por X segundos e depois terminar
            StartCoroutine(FreeMoveThenPauseAndOpenOptions());
        }
        else
        {
            // inicia pr�ximo passo automaticamente
            StartStep(currentStep);
        }
    }

    IEnumerator FreeMoveThenPauseAndOpenOptions()
    {
        // Hide narrator panel automatically
        if (narrator != null && narrator.panelToClose != null)
            narrator.panelToClose.SetActive(false);

        // Unlock full camera movement for free play
        if (camFollow != null)
        {
            camFollow.blockMovement = false;
            camFollow.blockZoom = false;
        }

        // Mensagem e painel durante o tempo livre
        if (stepMessageText != null)
            stepMessageText.text = $"Tutorial completed! You have {freeMoveDuration} seconds of free movement.";

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(true);

        float t = 0f;
        while (t < freeMoveDuration)
        {
            t += Time.deltaTime;
            if (stepMessageText != null)
                stepMessageText.text = $"Free Movement: {Mathf.CeilToInt(freeMoveDuration - t)}s";
            yield return null;
        }

        // Fecha o painel de tempo
        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        // Notifica final (opcional)
        onTutorialComplete?.Invoke();

        // Se ha escena siguiente, cargala; si no pausa el juego
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            var pauseMenu = FindObjectOfType<PauseMenu>();
            if (pauseMenu != null)
                pauseMenu.PauseGame();
            else
            {
                Time.timeScale = 0f;
                if (stepCompletePanel != null)
                    stepCompletePanel.SetActive(true);
            }
        }
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

        // calcula posi��o para a seta: um pouco � frente da c�mera na dire��o pedida
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

    // M�todos utilit�rios para debug / inspector
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