using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialStep1Manager : MonoBehaviour
{
    [Header("UI / Narrator")]
    public TutorialNarrator narrator;
    public TextMeshProUGUI stepMessageText;
    public GameObject stepCompletePanel;
    public string nextSceneName = "";

    [Header("Narrator texts")]
    [TextArea(2,3)] public string textSelectAndMove = "Click on a soldier to select it, or click and drag to select multiple at once! Then right-click to move them to the marked zone.";
    [TextArea(2,3)] public string textMoveToZone    = "Good! Now move your units to the marked zone!";
    [TextArea(2,3)] public string textZoneReached   = "Zone reached! Well done!";

    [Header("Units Arrow")]
    public GameObject arrowUnitsPrefab;
    public Transform  unitsAnchor;
    public Vector2    arrowUnitsOffset   = new Vector2(0f, 1.5f);
    public float      arrowUnitsRotation = 0f;

    [Header("Zone Arrow")]
    public GameObject arrowZonePrefab;
    public Transform  zoneAnchor;
    public Vector2    arrowZoneOffset   = new Vector2(0f, 1.5f);
    public float      arrowZoneRotation = 0f;

    [Header("Soldiers")]
    [Tooltip("How many soldiers must be selected before the move command advances the tutorial.")]
    public int totalSoldiersRequired = 1;

    [Header("Camera")]
    public cameraFollow cameraController;

    [Header("Completion")]
    public float completionDisplayDuration = 2f;

    int currentStep = 0;
    bool tutorialFinished = false;
    GameObject arrowUnitsGO;
    GameObject arrowZoneGO;

    void Start()
    {
        GameManager.tutorialMode = true;
        StartCoroutine(InitNextFrame());
    }

    IEnumerator InitNextFrame()
    {
        yield return null;
        if (stepCompletePanel != null) stepCompletePanel.SetActive(false);

        arrowUnitsGO = SpawnWorldArrow(arrowUnitsPrefab, unitsAnchor, arrowUnitsOffset, arrowUnitsRotation, "Units");
        arrowZoneGO  = SpawnWorldArrow(arrowZonePrefab,  zoneAnchor,  arrowZoneOffset,  arrowZoneRotation,  "Zone");

        StartStep(0);
    }

    GameObject SpawnWorldArrow(GameObject prefab, Transform anchor, Vector2 offset, float rotationZ, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[TutorialStep1Manager] Arrow prefab for '{label}' is not assigned.");
            return null;
        }
        var go = Instantiate(prefab);
        go.transform.rotation   = Quaternion.Euler(0f, 0f, rotationZ);
        go.transform.localScale = Vector3.one;

        if (anchor == null)
            Debug.LogWarning($"[TutorialStep1Manager] Anchor for '{label}' arrow is not assigned.");

        var pulse = go.GetComponent<WorldArrowPulse>() ?? go.AddComponent<WorldArrowPulse>();
        pulse.followTarget = anchor;
        pulse.followOffset = offset;

        go.SetActive(false);
        return go;
    }

    void OnEnable()
    {
        GameEvents.OnUnitsMoveCommand += HandleMoveCommand;
        GameEvents.OnUnitsSelected    += HandleUnitsSelected;
    }

    void OnDisable()
    {
        GameEvents.OnUnitsMoveCommand -= HandleMoveCommand;
        GameEvents.OnUnitsSelected    -= HandleUnitsSelected;
    }

    void HandleUnitsSelected(int count)
    {
        if (tutorialFinished || currentStep != 0) return;
        if (stepMessageText != null)
            stepMessageText.text = $"Selected {count}/{totalSoldiersRequired} — right-click to move them to the zone";
    }

    void HandleMoveCommand(int infantry, int tanks)
    {
        if (tutorialFinished || currentStep != 0) return;
        if (infantry + tanks < totalSoldiersRequired)
        {
            if (stepMessageText != null)
                stepMessageText.text = $"Select ALL {totalSoldiersRequired} soldiers first! (drag to select multiple)";
            return;
        }
        StartStep(1);
    }

    void StartStep(int step)
    {
        currentStep = step;
        switch (step)
        {
            case 0:
                ShowArrow(arrowUnitsGO);
                Say(textSelectAndMove);
                if (stepMessageText != null)
                    stepMessageText.text = $"Select all {totalSoldiersRequired} soldiers and move them to the zone";
                break;
            case 1:
                HideArrow(arrowUnitsGO);
                ShowArrow(arrowZoneGO);
                Say(textMoveToZone);
                if (stepMessageText != null) stepMessageText.text = "Reach the marked zone!";
                break;
        }
    }

    public void OnPlayerReachedTower() => OnPlayerReachedZone();

    public void OnPlayerReachedZone()
    {
        if (tutorialFinished) return;
        HideAllArrows();
        StartCoroutine(ShowCompletionThenProceed());
    }

    IEnumerator ShowCompletionThenProceed()
    {
        tutorialFinished = true;

        if (narrator != null && narrator.panelToClose != null)
            narrator.panelToClose.SetActive(false);

        if (stepCompletePanel != null) stepCompletePanel.SetActive(true);
        if (stepMessageText != null)   stepMessageText.text = textZoneReached;

        yield return new WaitForSeconds(completionDisplayDuration);

        if (stepCompletePanel != null) stepCompletePanel.SetActive(false);

        GameManager.tutorialMode = false;
        if (GameManager.Instance != null) GameManager.Instance.ResetGame();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    void Say(string text)
    {
        if (narrator == null) return;
        if (narrator.panelToClose != null) narrator.panelToClose.SetActive(true);
        narrator.ShowCustomText(text);
    }

    static void ShowArrow(GameObject go) { if (go != null) go.SetActive(true); }
    static void HideArrow(GameObject go) { if (go != null) go.SetActive(false); }

    void HideAllArrows()
    {
        HideArrow(arrowUnitsGO);
        HideArrow(arrowZoneGO);
    }

    public void SimulateComplete() => OnPlayerReachedZone();
}
