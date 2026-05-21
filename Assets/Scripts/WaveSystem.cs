using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Sistema de oleadas para el mapa final.
/// Ponlo en un GameObject vacío llamado "WaveSystem".
/// </summary>
public class WaveSystem : MonoBehaviour
{
    public static WaveSystem Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("Prefab del enemigo normal")]
    public GameObject enemyPrefab;
    [Tooltip("Prefab del tanque enemigo")]
    public GameObject tankEnemyPrefab;
    [Tooltip("Prefab del boss (mismo enemigo pero configurado diferente)")]
    public GameObject bossPrefab;

    [Header("Puntos de Spawn Aleatorios")]
    [Tooltip("Centro del área de spawn (normalmente el centro del mapa)")]
    public Vector2 spawnAreaCenter = Vector2.zero;
    [Tooltip("Tamaño del área de spawn (ancho x alto)")]
    public Vector2 spawnAreaSize = new Vector2(50f, 40f);
    [Tooltip("Radio mínimo desde el centro para evitar spawn encima del jugador")]
    public float minDistanceFromCenter = 15f;
    [Tooltip("1 punto de spawn exclusivo para el boss")]
    public Transform bossSpawnPoint;

    [Header("Configuración de Oleadas")]
    public int totalWaves = 5;
    public int enemiesPerWave = 15;
    [Tooltip("Enemigos extra por oleada")]
    public int enemiesIncreasePerWave = 5;
    [Tooltip("Tiempo entre spawns de enemigos")]
    public float timeBetweenSpawns = 0.5f;
    [Tooltip("Tiempo entre oleadas")]
    public float timeBetweenWaves = 10f;

    [Header("Dificultad Progresiva")]
    [Tooltip("Multiplicador de velocidad por oleada (ej: 0.15 = +15% por oleada)")]
    public float speedIncreasePerWave = 0.15f;
    [Tooltip("Multiplicador de vida por oleada (ej: 0.10 = +10% por oleada)")]
    public float healthIncreasePerWave = 0.10f;

    [Header("Preparación")]
    [Tooltip("Tiempo de preparación antes de la primera oleada")]
    public float preparationTime = 60f;

    [Header("UI")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI enemiesRemainingText;
    public TextMeshProUGUI timerText;
    public GameObject waveAnnouncementPanel;
    public TextMeshProUGUI waveAnnouncementText;

    [Header("Barra de vida del Boss")]
    public GameObject bossHealthBarPanel;
    public UnityEngine.UI.Slider bossHealthSlider;
    public TextMeshProUGUI bossHealthText;

    [Header("Victoria")]
    public GameObject victoryPanel;

    // Mensajes por oleada
    private readonly string[] waveTitles = {
        "Wave 1 - First Blood",
        "Wave 2 - They're Faster!",
        "Wave 3 - Tanks Incoming!",
        "Wave 4 - Maximum Force!",
        "⚠ BOSS WAVE ⚠"
    };

    private readonly string[] waveMessages = {
        "First wave incoming. Hold your ground!",
        "They're getting faster. Reinforce your defenses!",
        "Enemy tanks detected! Focus fire!",
        "They're giving everything. Don't let them through!",
        "THE COMMANDER APPROACHES. THIS IS IT!"
    };

    // Estado interno
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool waveInProgress = false;
    private bool gameStarted = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private GameObject bossInstance = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.staticMoney = true;

        if (bossHealthBarPanel != null)
            bossHealthBarPanel.SetActive(false);

        UpdateUI();
        StartCoroutine(PreparationPhase());
    }

    // --- FASE DE PREPARACIÓN ---
    IEnumerator PreparationPhase()
    {
        float timer = preparationTime;

        if (MissionNotifier.Instance != null)
            MissionNotifier.Instance.ShowMissionPriority(
                "Prepare your defenses!",
                $"You have {Mathf.RoundToInt(preparationTime)} seconds before the enemy attacks. Spend wisely!");

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = $"{Mathf.CeilToInt(timer)}s";
            yield return null;
        }

        if (timerText != null)
            timerText.text = "";

        gameStarted = true;
        StartCoroutine(StartNextWave());
    }

    // --- SIGUIENTE OLEADA ---
    IEnumerator StartNextWave()
    {
        currentWave++;

        if (currentWave > totalWaves)
        {
            Victory();
            yield break;
        }

        ShowWaveAnnouncement();
        yield return new WaitForSeconds(3f);

        int enemiesThisWave = enemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave;

        string title = currentWave <= waveTitles.Length ? waveTitles[currentWave - 1] : $"Wave {currentWave}!";
        string message = currentWave <= waveMessages.Length ? waveMessages[currentWave - 1] : $"Survive! {enemiesThisWave} enemies incoming.";

        if (MissionNotifier.Instance != null)
            MissionNotifier.Instance.ShowMissionPriority(title, message);

        if (currentWave == totalWaves && SoundColector.Instance != null)
            SoundColector.Instance.PlayVictoryMusic();

        UpdateUI();
        waveInProgress = true;

        yield return StartCoroutine(SpawnWave(enemiesThisWave));
    }

    // --- SPAWNEAR OLEADA CON POSICIONES ALEATORIAS ---
    IEnumerator SpawnWave(int totalEnemies)
    {
        if (currentWave == totalWaves)
        {
            yield return StartCoroutine(SpawnBoss());
            yield break;
        }

        // Composición por oleada
        int tankCount = 0;
        if (currentWave >= 3 && tankEnemyPrefab != null)
            tankCount = currentWave == 3 ? 5 : 10;

        int soldierCount = totalEnemies - tankCount;

        // Spawnear soldados en posiciones aleatorias
        for (int i = 0; i < soldierCount; i++)
        {
            SpawnEnemy(GetRandomSpawnPosition(), false, false);
            enemiesAlive++;
            UpdateEnemiesUI();
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        // Spawnear tanques en posiciones aleatorias
        for (int i = 0; i < tankCount; i++)
        {
            SpawnEnemy(GetRandomSpawnPosition(), false, true);
            enemiesAlive++;
            UpdateEnemiesUI();
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    // --- POSICIÓN ALEATORIA DE SPAWN ---
    Vector3 GetRandomSpawnPosition()
    {
        int groundLayer = LayerMask.GetMask("Ground");
        int attempts = 0;

        while (attempts < 30)
        {
            float x = spawnAreaCenter.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
            float y = spawnAreaCenter.y + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
            Vector3 pos = new Vector3(x, y, 0);

            // Verificar distancia mínima del centro
            if (Vector2.Distance(new Vector2(pos.x, pos.y), spawnAreaCenter) < minDistanceFromCenter)
            {
                attempts++;
                continue;
            }

            // Verificar que hay Ground debajo
            if (Physics2D.OverlapCircle(pos, 0.3f, groundLayer) != null)
                return pos;

            attempts++;
        }

        // Fallback: devolver el borde del área
        return new Vector3(spawnAreaCenter.x + spawnAreaSize.x / 2f, spawnAreaCenter.y, 0);
    }

    IEnumerator SpawnBoss()
    {
        Vector3 pos = bossSpawnPoint != null
            ? bossSpawnPoint.position
            : new Vector3(spawnAreaCenter.x, spawnAreaCenter.y + spawnAreaSize.y / 2f, 0);

        bossInstance = SpawnEnemy(pos, true, false);
        enemiesAlive++;
        UpdateEnemiesUI();

        if (bossHealthBarPanel != null)
            bossHealthBarPanel.SetActive(true);

        yield return null;
    }

    GameObject SpawnEnemy(Vector3 position, bool isBoss, bool isTank)
    {
        GameObject prefab = isBoss ? bossPrefab : (isTank && tankEnemyPrefab != null ? tankEnemyPrefab : enemyPrefab);
        if (prefab == null) return null;

        Vector2 offset = Random.insideUnitCircle * 1f;
        Vector3 spawnPos = position + new Vector3(offset.x, offset.y, 0);

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);

        // Aplicar dificultad progresiva
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.velocidad *= 1f + speedIncreasePerWave * (currentWave - 1);

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            int boostedHealth = Mathf.RoundToInt(health.maxHealth * (1f + healthIncreasePerWave * (currentWave - 1)));
            health.maxHealth = boostedHealth;
        }

        // Configurar IA
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.SetUsarPatrullaje(false);
            GameObject playerBase = GameObject.FindGameObjectWithTag("PlayerBase");
            if (playerBase != null) ai.baseJogador = playerBase.transform;
        }

        // Configurar boss
        if (isBoss)
        {
            enemy.transform.localScale = Vector3.one * 3f;

            BossExplosiveAttack bossAttack = enemy.GetComponent<BossExplosiveAttack>();
            if (bossAttack == null)
                bossAttack = enemy.AddComponent<BossExplosiveAttack>();

            if (bossHealthSlider != null && health != null)
            {
                bossHealthSlider.maxValue = health.maxHealth;
                bossHealthSlider.value = health.maxHealth;
            }
        }

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegistrarEnemy(enemy.GetComponent<EnemyAI>());

        return enemy;
    }

    // --- NOTIFICAR MUERTE DE ENEMIGO ---
    public void OnEnemyDied(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);

        if (enemy == bossInstance)
        {
            bossInstance = null;
            if (bossHealthBarPanel != null)
                bossHealthBarPanel.SetActive(false);
        }

        enemiesAlive--;
        if (enemiesAlive < 0) enemiesAlive = 0;
        UpdateEnemiesUI();

        if (enemiesAlive <= 0 && waveInProgress)
        {
            waveInProgress = false;
            StartCoroutine(WaitAndStartNextWave());
        }
    }

    // Llamado por EnemyHealth para actualizar la barra del boss
    public void UpdateBossHealthBar(int current, int max)
    {
        if (bossHealthSlider != null)
            bossHealthSlider.value = current;
        if (bossHealthText != null)
            bossHealthText.text = $"{current} / {max}";
    }

    IEnumerator WaitAndStartNextWave()
    {
        if (currentWave < totalWaves)
        {
            if (timerText != null)
            {
                float timer = timeBetweenWaves;
                while (timer > 0)
                {
                    timer -= Time.deltaTime;
                    timerText.text = $"{Mathf.CeilToInt(timer)}s";
                    yield return null;
                }
                timerText.text = "";
            }
            else
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        StartCoroutine(StartNextWave());
    }

    // --- UI ---
    void ShowWaveAnnouncement()
    {
        if (waveAnnouncementPanel == null) return;

        string text = currentWave == totalWaves ? "BOSS WAVE" : $"WAVE {currentWave}";

        if (waveAnnouncementText != null)
            waveAnnouncementText.text = text;

        waveAnnouncementPanel.SetActive(true);
        StartCoroutine(HideAnnouncement());
    }

    IEnumerator HideAnnouncement()
    {
        yield return new WaitForSeconds(3f);
        if (waveAnnouncementPanel != null)
            waveAnnouncementPanel.SetActive(false);
    }

    void UpdateUI()
    {
        if (waveText != null)
            waveText.text = $"{currentWave}";
    }

    void UpdateEnemiesUI()
    {
        if (enemiesRemainingText != null)
            enemiesRemainingText.text = $"{enemiesAlive}";
    }

    void Victory()
    {
        if (MissionNotifier.Instance != null)
            MissionNotifier.Instance.ShowMissionPriority("VICTORY!", "You have survived all waves. The enemy is defeated!");

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (LevelManager.Instance != null)
        {
            int lvl = LevelManager.Instance.CurrentLevel;
            LevelManager.Instance.MarkLevelCompleted(lvl);
        }

        Debug.Log("[WaveSystem] Victoria!");
    }

    // --- GIZMOS para ver el área de spawn en el editor ---
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawCube(new Vector3(spawnAreaCenter.x, spawnAreaCenter.y, 0), new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0));

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(new Vector3(spawnAreaCenter.x, spawnAreaCenter.y, 0), minDistanceFromCenter);
    }

    public int GetCurrentWave() => currentWave;
    public int GetEnemiesAlive() => enemiesAlive;
    public bool IsGameStarted() => gameStarted;
}