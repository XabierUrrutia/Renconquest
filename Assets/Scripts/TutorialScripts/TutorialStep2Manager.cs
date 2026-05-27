using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialStep2Manager : MonoBehaviour
{
    [Header("UI / Narrator")]
    public TutorialNarrator narrator;
    public TextMeshProUGUI stepMessageText;
    public GameObject stepCompletePanel;
    public string nextSceneName = "";

    [Header("Narrator texts")]
    [TextArea(2,3)] public string textSelectUnits = "Click on the soldier to select it!";
    [TextArea(2,3)] public string textAttackEnemy = "Right-click near the enemy to make your soldiers shoot at it!";
    [TextArea(2,3)] public string textEnemyDead   = "Enemy defeated! Great job!";

    [Header("Units Arrow")]
    public GameObject arrowUnitsPrefab;
    public Transform  unitsAnchor;
    public Vector2    arrowUnitsOffset   = new Vector2(0f, 1.5f);
    public float      arrowUnitsRotation = 0f;

    [Header("Enemy Arrow")]
    public GameObject arrowEnemyPrefab;
    public Vector2    arrowEnemyOffset   = new Vector2(0f, 1.5f);
    public float      arrowEnemyRotation = 0f;

    [Header("Enemy")]
    [Tooltip("Leave empty if the enemy spawns at runtime — it will be found automatically.")]
    public GameObject specificEnemy;
    [Tooltip("Tag used to find the enemy if Specific Enemy is empty.")]
    public string enemyTag = "Enemy";

    [Header("Soldiers")]
    [Tooltip("All soldiers must be selected before the tutorial advances to the attack step.")]
    public int totalSoldiersRequired = 1;

    [Header("Camera")]
    public cameraFollow cameraController;

    [Header("Completion")]
    public float completionDisplayDuration = 2f;

    int currentStep = 0;
    bool tutorialFinished = false;
    GameObject arrowUnitsGO;
    GameObject arrowEnemyGO;

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

        StartSpawnersInScene();
        StartStep(0);

        // Find enemy (already in scene or wait for spawner to create it)
        StartCoroutine(FindAndAttachEnemyArrow());
    }

    IEnumerator FindAndAttachEnemyArrow()
    {
        GameObject enemy = specificEnemy;

        if (enemy == null)
        {
            // Wait until the enemy spawns
            while (enemy == null)
            {
                // Try by EnemyDeathListener first (most reliable)
                var listener = FindObjectOfType<EnemyDeathListener>();
                if (listener != null)
                    enemy = listener.gameObject;

                // Fallback: find by tag
                if (enemy == null && !string.IsNullOrEmpty(enemyTag))
                    enemy = GameObject.FindGameObjectWithTag(enemyTag);

                if (enemy == null) yield return null;
            }
        }

        arrowEnemyGO = SpawnWorldArrow(arrowEnemyPrefab, enemy.transform, arrowEnemyOffset, arrowEnemyRotation, "Enemy");

        // If already on the attack step, show the arrow immediately
        if (currentStep == 1 && !tutorialFinished)
            ShowArrow(arrowEnemyGO);
    }

    GameObject SpawnWorldArrow(GameObject prefab, Transform anchor, Vector2 offset, float rotationZ, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[TutorialStep2Manager] Arrow prefab for '{label}' is not assigned.");
            return null;
        }
        var go = Instantiate(prefab);
        go.transform.rotation   = Quaternion.Euler(0f, 0f, rotationZ);
        go.transform.localScale = Vector3.one;

        if (anchor == null)
            Debug.LogWarning($"[TutorialStep2Manager] Anchor for '{label}' arrow is not assigned.");

        var pulse = go.GetComponent<WorldArrowPulse>() ?? go.AddComponent<WorldArrowPulse>();
        pulse.followTarget = anchor;
        pulse.followOffset = offset;

        go.SetActive(false);
        return go;
    }

    void OnEnable()  => GameEvents.OnUnitsSelected += HandleUnitsSelected;
    void OnDisable() => GameEvents.OnUnitsSelected -= HandleUnitsSelected;

    void HandleUnitsSelected(int count)
    {
        if (tutorialFinished || currentStep != 0) return;

        if (stepMessageText != null)
            stepMessageText.text = $"Selected {count}/{totalSoldiersRequired} soldiers";

        if (count >= totalSoldiersRequired)
            StartStep(1);
    }

    // Called directly by EnemyDeathListener
    public void OnEnemyDeath()
    {
        if (tutorialFinished) return;
        HideAllArrows();
        StartCoroutine(ShowCompletionThenProceed());
    }

    void StartStep(int step)
    {
        currentStep = step;
        switch (step)
        {
            case 0:
                ShowArrow(arrowUnitsGO);
                Say(textSelectUnits);
                if (stepMessageText != null)
                    stepMessageText.text = $"Select all {totalSoldiersRequired} soldiers (drag to select multiple)";
                break;
            case 1:
                HideArrow(arrowUnitsGO);
                ShowArrow(arrowEnemyGO);
                Say(textAttackEnemy);
                if (stepMessageText != null) stepMessageText.text = "Attack the enemy!";
                break;
        }
    }

    IEnumerator ShowCompletionThenProceed()
    {
        tutorialFinished = true;

        if (narrator != null && narrator.panelToClose != null)
            narrator.panelToClose.SetActive(false);

        if (stepCompletePanel != null) stepCompletePanel.SetActive(true);
        if (stepMessageText != null)   stepMessageText.text = textEnemyDead;

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
        HideArrow(arrowEnemyGO);
    }

    void StartSpawnersInScene()
    {
        foreach (var s in FindObjectsOfType<EnemySpawner>())
            if (s != null) s.StartSpawning();
    }

    public void SimulateEnemyDeath() => OnEnemyDeath();
}
