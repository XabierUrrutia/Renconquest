using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Ordene as cenas dos níveis (nível 1 -> nível 2 -> nível 3)")]
    public List<string> levelSceneNames = new List<string>();

    private const string KeyUnlocked = "UnlockedLevel";            // int: highest unlocked level (1-based)
    private const string KeyCompletedPrefix = "LevelCompleted_";   // LevelCompleted_{n} = 1
    private const string KeyDifficultyPrefix = "LevelDifficulty_";

    public int CurrentLevel { get; private set; } = 0; // 1-based; 0 = não é um nível configurado

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Garantir que este GameObject é root antes de torná-lo persistente
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Garantir que ao in?cio pelo menos o n?vel 1 esteja desbloqueado
        if (!PlayerPrefs.HasKey(KeyUnlocked))
            PlayerPrefs.SetInt(KeyUnlocked, 1);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        UpdateCurrentLevel(SceneManager.GetActiveScene());

    if (SoundColector.Instance != null)
    {
        if (CurrentLevel > 0) SoundColector.Instance.PlayGameplayMusic();
        else SoundColector.Instance.PlayMenuMusic();
    }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        UpdateCurrentLevel(scene);

        if (SoundColector.Instance != null)
        {
            if (CurrentLevel > 0) SoundColector.Instance.PlayGameplayMusic();
            else SoundColector.Instance.PlayMenuMusic();
        }
    }

    void UpdateCurrentLevel(Scene scene)
    {
        int idx = levelSceneNames.IndexOf(scene.name);
        CurrentLevel = idx >= 0 ? idx + 1 : 0;
    }

    // --- Queries por índice ---
    public int GetUnlockedLevel() => PlayerPrefs.GetInt(KeyUnlocked, 1);

    public bool IsUnlocked(int level) =>
        level >= 1 && level <= levelSceneNames.Count && level <= GetUnlockedLevel();

    public bool IsCompleted(int level) => PlayerPrefs.GetInt(KeyCompletedPrefix + level, 0) == 1;

    public Difficulty GetDifficulty(int level)
    {
        int val = PlayerPrefs.GetInt(KeyDifficultyPrefix + level, (int)Difficulty.Normal);
        return (Difficulty)Mathf.Clamp(val, 0, 2);
    }

    // --- Queries por nome de cena ---
    public int GetLevelIndexByScene(string sceneName)
    {
        int idx = levelSceneNames.IndexOf(sceneName);
        return idx >= 0 ? idx + 1 : 0;
    }

    // Retorna true se cena estiver desbloqueada ou não é um nível gerido por este LevelManager
    public bool IsSceneUnlocked(string sceneName)
    {
        int lvl = GetLevelIndexByScene(sceneName);
        if (lvl == 0) return true; // cena não é parte dos "níveis" — permitir
        return IsUnlocked(lvl);
    }

    // --- Modificadores ---
    public void SetDifficulty(int level, Difficulty difficulty)
    {
        if (level < 1 || level > levelSceneNames.Count) return;
        PlayerPrefs.SetInt(KeyDifficultyPrefix + level, (int)difficulty);
    }

    public void MarkLevelCompleted(int level)
    {
        if (level < 1 || level > levelSceneNames.Count) return;
        PlayerPrefs.SetInt(KeyCompletedPrefix + level, 1);
        PlayerPrefs.Save();
    }

    public void UnlockNextLevel(int completedLevel)
    {
        if (completedLevel < 1 || completedLevel >= levelSceneNames.Count) return;
        int unlocked = GetUnlockedLevel();
        int toUnlock = completedLevel + 1;
        if (toUnlock > unlocked)
        {
            PlayerPrefs.SetInt(KeyUnlocked, toUnlock);
            PlayerPrefs.Save();
            Debug.Log($"[LevelManager] Desbloqueado nível {toUnlock}");
        }
    }

    public void LoadLevel(int level)
    {
        if (level < 1 || level > levelSceneNames.Count)
        {
            Debug.LogWarning("[LevelManager] Level inválido: " + level);
            return;
        }

        if (!IsUnlocked(level))
        {
            Debug.Log("[LevelManager] Nível bloqueado: " + level);
            return;
        }

        string sceneName = levelSceneNames[level - 1];
        SceneManager.LoadScene(sceneName);
    }
}