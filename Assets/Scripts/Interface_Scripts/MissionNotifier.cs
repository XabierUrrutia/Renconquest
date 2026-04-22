using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente para mostrar miss�es/objetivos na tela durante o jogo.
/// Coloque este script num prefab UI (apenas UI) e atribua as refer�ncias no Inspector.
/// Use `EnqueueMission`, `ShowMission`, `CompleteCurrentMission` e `OnBuildingConquered` via c�digo para controlar as miss�es.
/// </summary>
public class MissionNotifier : MonoBehaviour
{
    // Singleton de conveni�ncia (opcional � facilita chamadas de outros scripts)
    public static MissionNotifier Instance { get; private set; }

    [Header("Refer�ncias de UI")]
    public GameObject panelRoot;                // root do painel (active/inactive)
    public TextMeshProUGUI titleText;           // t�tulo da miss�o
    public TextMeshProUGUI descriptionText;     // descri��o/objetivo
    public Button nextButton;                   // avan�a para a pr�xima miss�o / fecha
    public Button closeButton;                  // fecha o painel manualmente
    public Slider progressBar;                  // opcional: barra de progresso para a miss�o

    [Header("Comportamento")]
    public float autoHideSeconds = 5f;          // tempo at� esconder automaticamente (0 = nunca)
    public bool showNextAutomatically = true;   // mostra pr�xima miss�o automaticamente ao completar
    public bool hideOnComplete = true;          // esconder painel ao completar a miss�o

    [Header("Auto-show (in�cio do jogo)")]
    public bool showOnStart = true;
    [Tooltip("Si está activo, encola las misiones al inicio pero NO las muestra hasta que una zona las dispare con 'Use Next From Queue'.")]
    public bool enqueueOnStartWithoutShowing = false;
    [TextArea] public string initialMissionTitle;
    [TextArea] public string initialMissionDescription;

    [Header("Miss�es edit�veis no Inspector")]
    public List<MissionEntry> inspectorMissions = new List<MissionEntry>();

    [Header("Templates")]
    [Tooltip("Template para t�tulo quando um edif�cio � conquistado. Use {0} para o nome do edif�cio.")]
    public string conquestTitleTemplate = "Edif�cio conquistado: {0}";
    [Tooltip("Template para descri��o quando um edif�cio � conquistado. Use {0} para o nome do edif�cio.")]
    [TextArea] public string conquestDescriptionTemplate = "Voc� conquistou {0}. Recolha recursos e defenda a posi��o.";

    private Queue<Mission> queue = new Queue<Mission>();
    private Mission? current;
    private Coroutine autoHideCoroutine;
    private float currentProgress = 0f;

    [System.Serializable]
    public class MissionEntry
    {
        public string title;
        [TextArea] public string description;
        public bool optional;
    }

    public struct Mission
    {
        public string title;
        public string description;
        public bool isOptional;

        public Mission(string title, string description, bool optional = false)
        {
            this.title = title ?? "";
            this.description = description ?? "";
            this.isOptional = optional;
        }
    }

    void Awake()
    {
        // Singleton (n�o destrua se j� existir outra inst�ncia)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MissionNotifier] Mais de uma inst�ncia encontrada. Esta inst�ncia ser� destru�da.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePanel);
            closeButton.onClick.AddListener(HidePanel);
        }

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = currentProgress;
        }
    }

    void Start()
    {
        if (showOnStart && inspectorMissions != null && inspectorMissions.Count > 0)
        {
            if (enqueueOnStartWithoutShowing)
            {
                // Mete todas en la cola sin mostrar ninguna — las zonas las disparan
                foreach (var me in inspectorMissions)
                    queue.Enqueue(new Mission(me.title, me.description, me.optional));
            }
            else
            {
                foreach (var me in inspectorMissions)
                    EnqueueMission(new Mission(me.title, me.description, me.optional));
            }
        }
        else if (showOnStart && !string.IsNullOrWhiteSpace(initialMissionTitle))
        {
            if (enqueueOnStartWithoutShowing)
                queue.Enqueue(new Mission(initialMissionTitle, initialMissionDescription, false));
            else
                EnqueueMission(new Mission(initialMissionTitle, initialMissionDescription, false));
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
        if (closeButton != null) closeButton.onClick.RemoveListener(HidePanel);
    }

    // Public API --------------------------------------------------------

    // Chamado por outros scripts quando um edif�cio � conquistado.
    // Ex.: BuildingOwnership deve chamar: MissionNotifier.Instance.OnBuildingConquered(gameObject.name);
    public void OnBuildingConquered(string buildingName, bool showImmediately = true)
    {
        string title = string.Format(conquestTitleTemplate, buildingName);
        string desc = string.Format(conquestDescriptionTemplate, buildingName);
        if (showImmediately)
            ShowMission(title, desc, false);
        else
            EnqueueMission(title, desc, false);
    }

    // Enfileira uma miss�o; se n�o houver miss�o ativa, mostra-a
    public void EnqueueMission(string title, string description, bool optional = false)
    {
        EnqueueMission(new Mission(title, description, optional));
    }

    public void EnqueueMission(Mission mission)
    {
        queue.Enqueue(mission);
        if (current == null)
            ShowNextFromQueue();
    }

    // Mostra imediatamente uma miss�o (prioriza sobre a atual)
    public void ShowMission(string title, string description, bool optional = false)
    {
        current = new Mission(title, description, optional);
        UpdateUIForCurrent();
    }

    // Marca a miss�o atual como conclu�da e avan�a
    public void CompleteCurrentMission()
    {
        if (current == null) return;

        current = null;
        currentProgress = 0f;
        if (progressBar != null) progressBar.value = currentProgress;

        if (showNextAutomatically)
            ShowNextFromQueue();
        else if (hideOnComplete)
            HidePanel();
    }

    // Remove todas as miss�es pendentes e oculta
    public void ClearAll()
    {
        queue.Clear();
        current = null;
        HidePanel();
    }

    // Atualiza o progresso visual da miss�o atual (0..1)
    public void SetProgress(float normalized)
    {
        currentProgress = Mathf.Clamp01(normalized);
        if (progressBar != null)
            progressBar.value = currentProgress;
    }

    // Internal helpers -------------------------------------------------

    private void ShowNextFromQueue()
    {
        if (queue.Count == 0)
        {
            HidePanel();
            return;
        }

        current = queue.Dequeue();
        UpdateUIForCurrent();
    }

    private void UpdateUIForCurrent()
    {
        if (current == null)
        {
            HidePanel();
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        if (titleText != null) titleText.text = current.Value.title;
        if (descriptionText != null) descriptionText.text = current.Value.description;

        // reset progress visual quando uma miss�o nova aparece
        currentProgress = 0f;
        if (progressBar != null) progressBar.value = currentProgress;

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (autoHideSeconds > 0f)
            autoHideCoroutine = StartCoroutine(AutoHideCoroutine(autoHideSeconds));
    }

    private IEnumerator AutoHideCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        autoHideCoroutine = null;
        current = null;

        if (showNextAutomatically && queue.Count > 0)
            ShowNextFromQueue();
        else
            HidePanel();
    }

    private void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    private void OnNextClicked()
    {
        // Avan�a sempre para a pr�xima miss�o quando o jogador clica em Next
        CompleteCurrentMission();
    }

    // Muestra inmediatamente interrumpiendo la cola actual
    public void ShowMissionPriority(string title, string description)
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        current = new Mission(title, description, false);
        UpdateUIForCurrent();
    }

    // Saca la siguiente misión de la cola y la muestra — llamado por MissionTriggerZone
    public void TriggerNextQueued()
    {
        if (queue.Count == 0) return;

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        current = null;
        ShowNextFromQueue();
    }

    // Query helpers ----------------------------------------------------

    public bool HasActiveMission() => current != null;
    public int PendingCount() => queue.Count;
    public Mission? GetCurrentMission() => current;
}