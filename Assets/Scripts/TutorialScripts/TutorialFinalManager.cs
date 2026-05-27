using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialFinalManager : MonoBehaviour
{
    [Header("UI / Narrator")]
    public TutorialNarrator narrator;
    public TextMeshProUGUI stepMessageText;
    public GameObject stepCompletePanel;
    public string nextSceneName = "";

    [Header("Narrator texts (one per arrow)")]
    [TextArea(2,3)] public string textArrowBuildButton  = "Open the building shop to start constructing!";
    [TextArea(2,3)] public string textArrowTower        = "Select the Tower to defend your base!";
    [TextArea(2,3)] public string textArrowBuildButton2 = "Great! Now open the shop again to build an Arsenal.";
    [TextArea(2,3)] public string textArrowArsenal      = "Select the Arsenal to train soldiers!";
    [TextArea(2,3)] public string textArrowArsenalWorld = "Click on the Arsenal you just placed.";
    [TextArea(2,3)] public string textArrowRecruit      = "Recruit a soldier from the Arsenal!";

    [Header("Completion")]
    public float completionDisplayDuration = 3f;
    [Tooltip("Seconds of free time to recruit more soldiers before loading next scene. 0 = skip.")]
    public float freeRecruitDuration = 30f;

    [Header("Tutorial Enemy")]
    [Tooltip("Starts disabled. Activates after tower is placed.")]
    public GameObject tutorialEnemy;

    [Header("Build button (HUD)")]
    public Button buildButton;
    [Tooltip("The building shop panel. Used to sync arrows with panel open/close state.")]
    public GameObject buildingShopPanel;

    [Header("Arrows")]
    [Tooltip("Points to the Build button in the HUD.")]
    public TutorialArrow arrowBuildButton;
    [Tooltip("Points to the Tower button inside the building shop.")]
    public TutorialArrow arrowTower;
    [Tooltip("Points to the Arsenal button inside the building shop.")]
    public TutorialArrow arrowArsenal;
    [Tooltip("Points to the placed Arsenal building in the world (to click it).")]
    public TutorialArrow arrowArsenalWorld;
    [Tooltip("Points to the recruit button inside the barracks panel.")]
    public TutorialArrow arrowRecruit;

    [Header("Placement Zones")]
    [Tooltip("Circle on the ground showing where to place the Tower.")]
    public TutorialPlacementZone zoneTower;
    [Tooltip("Circle on the ground showing where to place the Arsenal.")]
    public TutorialPlacementZone zoneArsenal;

    [Header("Camera")]
    [Tooltip("Assign the cameraFollow script to lock camera movement during the tutorial.")]
    public cameraFollow cameraController;

    [Header("Barracks Panel")]
    [Tooltip("PanelSoldadosUI — used to detect when it opens/closes.")]
    public GameObject barracksPanel;

    [Header("Events")]
    public UnityEvent onTutorialComplete;

    // 0 = place tower, 1 = place arsenal, 2 = recruit soldier
    int currentStep = 0;
    bool waitingForAction = false;
    bool tutorialFinished = false;
    bool enemyDied = false;
    bool barracksPanelWasOpen = false;
    bool shopPanelWasOpen = false;

    static readonly string[] stepMessages = new string[3]
    {
        "Step 1/3: open the shop and place a Tower",
        "Step 2/3: open the shop and place an Arsenal",
        "Step 3/3: click the Arsenal and recruit a soldier"
    };

    void Start()
    {
        GameManager.tutorialMode = true;
        StartCoroutine(InitNextFrame());
    }

    System.Collections.IEnumerator InitNextFrame()
    {
        yield return null; // wait one frame so all other Start() methods run first

        if (stepCompletePanel != null) stepCompletePanel.SetActive(false);
        if (tutorialEnemy != null)     tutorialEnemy.SetActive(false);

        HideAllArrows();

        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildButtonClicked);

        shopPanelWasOpen = buildingShopPanel != null && buildingShopPanel.activeSelf;

        StartStep(0);
    }

    void OnDestroy()
    {
        if (buildButton != null)
            buildButton.onClick.RemoveListener(OnBuildButtonClicked);
    }

    void OnEnable()
    {
        GameEvents.OnBuildingPlaced    += HandleBuildingPlaced;
        GameEvents.OnTutorialEnemyDied += HandleEnemyDied;
        GameEvents.OnSoldierRecruited  += HandleSoldierRecruited;
    }

    void OnDisable()
    {
        GameEvents.OnBuildingPlaced    -= HandleBuildingPlaced;
        GameEvents.OnTutorialEnemyDied -= HandleEnemyDied;
        GameEvents.OnSoldierRecruited  -= HandleSoldierRecruited;
    }

    void Update()
    {
        // Shop panel open/close tracking (steps 0 and 1)
        if (!tutorialFinished && buildingShopPanel != null && currentStep < 2)
        {
            bool shopOpen = buildingShopPanel.activeSelf;

            if (shopOpen != shopPanelWasOpen)
            ShowBuildOrShopArrow();

            shopPanelWasOpen = shopOpen;
        }

        // Placement zone: only visible while actively placing the correct building
        if (!tutorialFinished && (currentStep == 0 || currentStep == 1))
        {
            bool isPlacing = BuildingManager.Instance != null && BuildingManager.Instance.IsPlacing();
            TutorialPlacementZone zone = currentStep == 0 ? zoneTower : zoneArsenal;
            if (zone != null)
            {
                if (isPlacing && !zone.gameObject.activeSelf)  zone.gameObject.SetActive(true);
                else if (!isPlacing && zone.gameObject.activeSelf) zone.Hide();
            }
        }

        if (tutorialFinished || currentStep != 2 || barracksPanel == null) return;

        bool isOpen = barracksPanel.activeSelf;

        if (isOpen && !barracksPanelWasOpen)
        {
            // Panel just opened
            if (arrowArsenalWorld != null) arrowArsenalWorld.Hide();
            if (arrowRecruit != null)      { arrowRecruit.Show(); Say(textArrowRecruit); }
        }
        else if (!isOpen && barracksPanelWasOpen)
        {
            // Panel just closed
            if (arrowRecruit != null)      arrowRecruit.Hide();
            if (arrowArsenalWorld != null) arrowArsenalWorld.Show();
        }

        barracksPanelWasOpen = isOpen;
    }

    void OnBuildButtonClicked()
    {
        if (tutorialFinished) return;

        if (currentStep == 0 && !enemyDied)
        {
            // Step 0: shop opens for tower
            if (arrowBuildButton != null) arrowBuildButton.Hide();
            if (arrowTower != null)       arrowTower.Show();
        }
        else if (currentStep == 1)
        {
            // Step 1: shop opens for arsenal
            if (arrowBuildButton != null) arrowBuildButton.Hide();
            if (arrowArsenal != null)     arrowArsenal.Show();
        }
    }

    void HandleBuildingPlaced(Transform placed)
    {
        if (tutorialFinished) return;

        if (currentStep == 0 && !enemyDied)
        {
            if (arrowTower != null) arrowTower.Hide();
            if (zoneTower != null) zoneTower.Hide();
            if (tutorialEnemy != null) tutorialEnemy.SetActive(true);
            waitingForAction = false;
        }
        else if (currentStep == 1)
        {
            if (arrowArsenal != null) arrowArsenal.Hide();
            if (zoneArsenal != null) zoneArsenal.Hide();
            if (arrowArsenalWorld != null)
            {
                // Make the arrow follow the placed building
                if (placed != null) arrowArsenalWorld.followWorldTarget = placed;
                arrowArsenalWorld.Show();
                Say(textArrowArsenalWorld);
            }
            CompleteCurrentStep();
        }
    }

    void HandleEnemyDied()
    {
        if (tutorialFinished || currentStep != 0) return;
        enemyDied = true;
        if (arrowBuildButton != null) arrowBuildButton.ResetDone();
        CompleteCurrentStep(); // advance to step 1 — StartStep will call ShowBuildOrShopArrow
    }

    void HandleSoldierRecruited()
    {
        if (!waitingForAction || tutorialFinished || currentStep != 2) return;
        if (arrowRecruit != null)      arrowRecruit.Hide();
        if (arrowArsenalWorld != null) arrowArsenalWorld.Hide();
        CompleteCurrentStep();
    }

    void StartStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= stepMessages.Length)
        {
            EndTutorial();
            return;
        }

        currentStep = stepIndex;
        waitingForAction = true;

        if (stepIndex == 0 || stepIndex == 1)
            ShowBuildOrShopArrow();


        if (stepMessageText != null)
            stepMessageText.text = stepMessages[stepIndex];
    }

    void Say(string text)
    {
        if (narrator == null) return;
        if (narrator.panelToClose != null) narrator.panelToClose.SetActive(true);
        narrator.ShowCustomText(text);
    }

    // Shows arrowBuildButton OR the in-shop arrow depending on whether the shop is already open
    void ShowBuildOrShopArrow()
    {
        bool shopOpen = buildingShopPanel != null && buildingShopPanel.activeSelf;

        if (shopOpen)
        {
            if (arrowBuildButton != null) arrowBuildButton.Hide();
            if (currentStep == 0 && arrowTower != null)   { arrowTower.Show();   Say(textArrowTower); }
            if (currentStep == 1 && arrowArsenal != null) { arrowArsenal.Show(); Say(textArrowArsenal); }
        }
        else
        {
            if (arrowTower != null)   arrowTower.Hide();
            if (arrowArsenal != null) arrowArsenal.Hide();
            if (arrowBuildButton != null)
            {
                arrowBuildButton.Show();
                Say(currentStep == 1 ? textArrowBuildButton2 : textArrowBuildButton);
            }
        }
    }

    void CompleteCurrentStep()
    {
        waitingForAction = false;
        int next = currentStep + 1;

        if (next >= stepMessages.Length)
            StartCoroutine(ShowCompletionThenProceed());
        else
            StartStep(next);
    }

    IEnumerator ShowCompletionThenProceed()
    {
        tutorialFinished = true;

        if (cameraController != null) cameraController.blockMovement = false;

        if (narrator != null && narrator.panelToClose != null)
            narrator.panelToClose.SetActive(false);

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(true);

        onTutorialComplete.Invoke();

        // Free recruit time with countdown
        if (freeRecruitDuration > 0f)
        {
            float t = 0f;
            while (t < freeRecruitDuration)
            {
                t += Time.deltaTime;
                if (stepMessageText != null)
                    stepMessageText.text = $"Tutorial complete! Keep recruiting soldiers: {Mathf.CeilToInt(freeRecruitDuration - t)}s";
                yield return null;
            }
        }
        else
        {
            if (stepMessageText != null)
                stepMessageText.text = "Tutorial complete! You are ready to play!";
            yield return new WaitForSeconds(completionDisplayDuration);
        }

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    void EndTutorial()
    {
        tutorialFinished = true;
        if (stepCompletePanel != null) stepCompletePanel.SetActive(false);
        onTutorialComplete.Invoke();
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    void HideAllArrows()
    {
        if (arrowBuildButton != null) arrowBuildButton.Hide();
        if (arrowTower != null)       arrowTower.Hide();
        if (arrowArsenal != null)     arrowArsenal.Hide();
        if (arrowArsenalWorld != null) arrowArsenalWorld.Hide();
        if (arrowRecruit != null)     arrowRecruit.Hide();
        if (zoneTower != null)        zoneTower.Hide();
        if (zoneArsenal != null)      zoneArsenal.Hide();
    }

    public void SimulateStep()
    {
        if (!tutorialFinished && waitingForAction) CompleteCurrentStep();
    }
}