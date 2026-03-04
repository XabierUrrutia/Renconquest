using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente para mostrar missões/objetivos na tela durante o jogo.
/// Coloque este script num prefab UI (apenas UI) e atribua as referências no Inspector.
/// Use `EnqueueMission`, `ShowMission`, `CompleteCurrentMission` e `OnBuildingConquered` via código para controlar as missões.
/// </summary>
public class MissionNotifier : MonoBehaviour
{
    // Singleton de conveniência (opcional — facilita chamadas de outros scripts)
    public static MissionNotifier Instance { get; private set; }

    [Header("Referências de UI")]
    public GameObject panelRoot;                // root do painel (active/inactive)
    public TextMeshProUGUI titleText;           // título da missão
    public TextMeshProUGUI descriptionText;     // descrição/objetivo
    public Button nextButton;                   // avança para a próxima missão / fecha
    public Button closeButton;                  // fecha o painel manualmente
    public Slider progressBar;                  // opcional: barra de progresso para a missão

    [Header("Comportamento")]
    public float autoHideSeconds = 5f;          // tempo até esconder automaticamente (0 = nunca)
    public bool showNextAutomatically = true;   // mostra próxima missão automaticamente ao completar
    public bool hideOnComplete = true;          // esconder painel ao completar a missão

    [Header("Auto-show (início do jogo)")]
    public bool showOnStart = true;             // mostrar missões definidas no inspetor ao arrancar
    [TextArea] public string initialMissionTitle;
    [TextArea] public string initialMissionDescription;

    [Header("Missões editáveis no Inspector")]
    public List<MissionEntry> inspectorMissions = new List<MissionEntry>();

    [Header("Templates")]
    [Tooltip("Template para título quando um edifício é conquistado. Use {0} para o nome do edifício.")]
    public string conquestTitleTemplate = "Edifício conquistado: {0}";
    [Tooltip("Template para descrição quando um edifício é conquistado. Use {0} para o nome do edifício.")]
    [TextArea] public string conquestDescriptionTemplate = "Você conquistou {0}. Recolha recursos e defenda a posição.";

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
        // Singleton (não destrua se já existir outra instância)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MissionNotifier] Mais de uma instância encontrada. Esta instância será destruída.");
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
        // Se houver missões definidas na lista do Inspector e showOnStart = true, enfileira-as
        if (showOnStart && inspectorMissions != null && inspectorMissions.Count > 0)
        {
            foreach (var me in inspectorMissions)
                EnqueueMission(new Mission(me.title, me.description, me.optional));
        }
        else if (showOnStart && !string.IsNullOrWhiteSpace(initialMissionTitle))
        {
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

    // Chamado por outros scripts quando um edifício é conquistado.
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

    // Enfileira uma missão; se não houver missão ativa, mostra-a
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

    // Mostra imediatamente uma missão (prioriza sobre a atual)
    public void ShowMission(string title, string description, bool optional = false)
    {
        current = new Mission(title, description, optional);
        UpdateUIForCurrent();
    }

    // Marca a missão atual como concluída e avança
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

    // Remove todas as missões pendentes e oculta
    public void ClearAll()
    {
        queue.Clear();
        current = null;
        HidePanel();
    }

    // Atualiza o progresso visual da missão atual (0..1)
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

        // reset progress visual quando uma missão nova aparece
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
        HidePanel();
        autoHideCoroutine = null;
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
        // Avança sempre para a próxima missão quando o jogador clica em Next
        CompleteCurrentMission();
    }

    // Query helpers ----------------------------------------------------

    public bool HasActiveMission() => current != null;
    public int PendingCount() => queue.Count;
    public Mission? GetCurrentMission() => current;
}