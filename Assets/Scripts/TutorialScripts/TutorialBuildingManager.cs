using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialBuildingManager : MonoBehaviour
{
    [Header("UI / Narrator")]
    public TutorialNarrator narrator;
    public TextMeshProUGUI stepMessageText;
    public GameObject stepCompletePanel;
    public string nextSceneName = "";

    [Header("Narrator texts")]
    [TextArea(2,3)] public string textSelectSoldiers = "Select your soldiers to start the conquest!";
    [TextArea(2,3)] public string textMoveToBuilding = "Now move your soldiers to the building to capture it!";

    [Header("Message")]
    public string instructionText = "Capture the building to continue!";

    [Header("Soldiers")]
    [Tooltip("Minimum soldiers that must be selected to advance the narrator.")]
    public int minUnitsToAdvance = 1;

    [Header("Camera")]
    [Tooltip("Assign the cameraFollow to lock camera until the title screen disappears.")]
    public cameraFollow cameraController;
    [Tooltip("Assign the TutorialTitleScreen so the camera unlocks when the title hides.")]
    public TutorialTitleScreen titleScreen;

    [Header("Completion")]
    public float completionDisplayDuration = 2f;

    [Header("Events")]
    public UnityEvent onTutorialComplete;

    bool done = false;
    bool narratorAdvanced = false;

    void Start()
    {
        GameManager.tutorialMode = true;

        if (stepCompletePanel != null) stepCompletePanel.SetActive(false);
        if (stepMessageText != null)   stepMessageText.text = instructionText;

        if (cameraController != null)
        {
            cameraController.blockMovement = true;
            cameraController.blockZoom = true;
        }

        if (titleScreen != null)
            StartCoroutine(WaitForTitleThenUnlockCamera());

        Say(textSelectSoldiers);
    }

    IEnumerator WaitForTitleThenUnlockCamera()
    {
        while (titleScreen != null && titleScreen.titlePanel != null && titleScreen.titlePanel.activeSelf)
            yield return null;

        yield return null;

        if (cameraController != null)
        {
            cameraController.blockMovement = false;
            cameraController.blockZoom = false;
        }
    }

    void OnEnable()
    {
        GameEvents.OnBuildingCaptured += HandleBuildingCaptured;
        GameEvents.OnUnitsSelected    += HandleUnitsSelected;
    }

    void OnDisable()
    {
        GameEvents.OnBuildingCaptured -= HandleBuildingCaptured;
        GameEvents.OnUnitsSelected    -= HandleUnitsSelected;
    }

    void HandleUnitsSelected(int count)
    {
        if (done || narratorAdvanced || count < minUnitsToAdvance) return;
        narratorAdvanced = true;
        Say(textMoveToBuilding);
    }

    void HandleBuildingCaptured()
    {
        if (done) return;
        done = true;
        StartCoroutine(CompleteAndProceed());
    }

    IEnumerator CompleteAndProceed()
    {
        if (narrator != null && narrator.panelToClose != null)
            narrator.panelToClose.SetActive(false);

        if (stepMessageText != null) stepMessageText.text = "Building captured! Well done!";
        if (stepCompletePanel != null) stepCompletePanel.SetActive(true);

        onTutorialComplete.Invoke();

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

    public void SimulateCapture() => HandleBuildingCaptured();
}
