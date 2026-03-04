using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Painel do menu de pausa
    public bool isPaused = false;

    void Start()
    {
        // Garante que o jogo começa sem estar em pausa
        ResumeGame();
    }

    void Update()
    {
        // Também podes permitir pausar com a tecla Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        SoundColector.Instance?.PlayUiClick();
        SoundColector.Instance?.PlayPauseMusic();


        Time.timeScale = 0f;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerPositionManager.SavePosition(player.transform.position);
        }

        pauseMenuUI.SetActive(true);
    }

    public void ResumeGame()
    {
        SoundColector.Instance?.PlayUiClick();
        SoundColector.Instance?.PlayGameplayMusic();

        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;  // retoma o jogo
        isPaused = false;
    }

    public void GoToMainMenu()
    {
        SoundColector.Instance?.PlayMenuMusic();

        Time.timeScale = 1f; // volta ao normal antes de trocar de cena
        SceneManager.LoadScene(0); // exemplo: menu principal
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }
}
