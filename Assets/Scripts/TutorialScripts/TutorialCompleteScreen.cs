using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialCompleteScreen : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";
    public string firstMapScene = "first map";

    void Start()
    {
        // Keep tutorialMode true so GameManager doesn't trigger Game Over on this screen
        GameManager.tutorialMode = true;
        if (GameManager.Instance != null) GameManager.Instance.ResetGame();

        if (titleText != null)    titleText.text    = "Tutorial Complete!";
        if (subtitleText != null) subtitleText.text = "You are now ready to conquer Portugal!";
    }

    // Call these from the Button's OnClick() list in the Inspector
    public void GoToMainMenu()
    {
        GameManager.tutorialMode = false;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void StartGame()
    {
        GameManager.tutorialMode = false;
        SceneManager.LoadScene(firstMapScene);
    }
}
