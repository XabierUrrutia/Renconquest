using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyWaveConfig
{
    public string waveName = "Oleada";
    public int totalEnemies = 10;
    public int enemiesPerWave = 3;
    public float timeBetweenSpawns = 2f;
}

public class EnemyWaveManager : MonoBehaviour
{
    public static EnemyWaveManager Instance;

    [Header("Configuración de Oleadas")]
    public List<EnemyWaveConfig> waveConfigs = new List<EnemyWaveConfig>();
    public float timeBetweenWaves = 30f;
    public bool startOnAwake = true;
    public bool waitForAllEnemiesDead = true; // Nueva opción

    [Header("Referencias")]
    public List<EnemySpawner> enemySpawners = new List<EnemySpawner>();
    public EnemyBase enemyBase;

    [Header("Oleada de Venganza")]
    public bool revengeWaveEnabled = true;
    public int revengeWaveEnemies = 20;
    public int revengeWaveEnemiesPerWave = 8;
    public float revengeWaveSpawnRate = 1f;

    // Variables de estado
    private int currentWave = 0;
    private float nextWaveTimer = 0f;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool revengeWaveActive = false;
    private bool waveInProgress = false;
    private bool allSpawnersFinished = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (waveConfigs.Count == 0)
        {
            CreateDefaultWaveConfigs();
        }

        if (enemySpawners.Count == 0)
        {
            FindAllEnemySpawners();
        }

        if (enemyBase == null)
        {
            enemyBase = FindObjectOfType<EnemyBase>();
        }

        if (startOnAwake)
        {
            StartWaves();
        }
    }

    void Update()
    {
        if (!waveInProgress) return;

        // Verificar si la base enemiga ha sido conquistada
        if (enemyBase != null && enemyBase.isConquered && !revengeWaveActive && revengeWaveEnabled)
        {
            StartRevengeWave();
            return;
        }

        // Si estamos esperando que mueran todos los enemigos
        if (waitForAllEnemiesDead && allSpawnersFinished)
        {
            // Verificar si no quedan enemigos
            if (activeEnemies.Count == 0)
            {
                WaveCompleted();
            }
        }
        // Si no estamos esperando, usar temporizador normal
        else if (!waitForAllEnemiesDead)
        {
            nextWaveTimer -= Time.deltaTime;
            if (nextWaveTimer <= 0f)
            {
                StartNextWave();
            }
        }
    }

    void CreateDefaultWaveConfigs()
    {
        waveConfigs = new List<EnemyWaveConfig>
        {
            new EnemyWaveConfig { waveName = "Oleada 1", totalEnemies = 5, enemiesPerWave = 2, timeBetweenSpawns = 3f },
            new EnemyWaveConfig { waveName = "Oleada 2", totalEnemies = 8, enemiesPerWave = 3, timeBetweenSpawns = 2.5f },
            new EnemyWaveConfig { waveName = "Oleada 3", totalEnemies = 12, enemiesPerWave = 4, timeBetweenSpawns = 2f },
            new EnemyWaveConfig { waveName = "Oleada 4", totalEnemies = 15, enemiesPerWave = 5, timeBetweenSpawns = 1.5f },
            new EnemyWaveConfig { waveName = "Oleada 5", totalEnemies = 20, enemiesPerWave = 6, timeBetweenSpawns = 1f }
        };
    }

    void FindAllEnemySpawners()
    {
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        enemySpawners.Clear();
        enemySpawners.AddRange(spawners);

        Debug.Log($"Encontrados {enemySpawners.Count} spawners de enemigos");
    }

    public void StartWaves()
    {
        waveInProgress = true;
        currentWave = 0;
        nextWaveTimer = 5f;
        revengeWaveActive = false;
        allSpawnersFinished = false;

        Debug.Log("Sistema de oleadas iniciado");
        StartNextWave();
    }

    public void StopWaves()
    {
        waveInProgress = false;
        revengeWaveActive = false;
        allSpawnersFinished = false;

        foreach (EnemySpawner spawner in enemySpawners)
        {
            if (spawner != null)
            {
                spawner.StopSpawning();
            }
        }

        Debug.Log("Sistema de oleadas detenido");
    }

    void StartNextWave()
    {
        if (currentWave >= waveConfigs.Count)
        {
            currentWave = 0;
            IncreaseDifficulty();
        }

        EnemyWaveConfig waveConfig = waveConfigs[currentWave];
        StartWave(waveConfig);

        Debug.Log($"Oleada {currentWave + 1} iniciada. Enemigos: {waveConfig.totalEnemies}");
    }

    void StartWave(EnemyWaveConfig config)
    {
        waveInProgress = true;
        allSpawnersFinished = false;

        // Reiniciar temporizador
        nextWaveTimer = timeBetweenWaves;

        // Activar todos los spawners
        bool anySpawnerActive = false;
        foreach (EnemySpawner spawner in enemySpawners)
        {
            if (spawner != null && spawner.gameObject.activeInHierarchy)
            {
                spawner.SetWaveParameters(config.totalEnemies, config.enemiesPerWave, config.timeBetweenSpawns);
                spawner.ResetSpawner();
                spawner.StartSpawning();
                anySpawnerActive = true;
            }
        }

        if (!anySpawnerActive)
        {
            Debug.LogWarning("No hay spawners activos para iniciar la oleada");
            WaveCompleted();
        }
    }

    void StartRevengeWave()
    {
        if (!revengeWaveEnabled) return;

        revengeWaveActive = true;
        allSpawnersFinished = false;

        EnemyWaveConfig revengeConfig = new EnemyWaveConfig
        {
            waveName = "¡VENGANZA!",
            totalEnemies = revengeWaveEnemies,
            enemiesPerWave = revengeWaveEnemiesPerWave,
            timeBetweenSpawns = revengeWaveSpawnRate
        };

        StartWave(revengeConfig);

        Debug.Log("¡OLEADA DE VENGANZA INICIADA!");

        foreach (GameObject enemy in activeEnemies)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                Transform player = FindNearestPlayer(enemy.transform.position);
                if (player != null)
                {
                    enemyAI.SetUsarPatrullaje(false);
                    enemyAI.AdicionarJogador(player);
                }
            }
        }
    }

    Transform FindNearestPlayer(Vector3 position)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearestPlayer = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestPlayer = player.transform;
            }
        }

        return nearestPlayer;
    }

    void WaveCompleted()
    {
        waveInProgress = false;
        allSpawnersFinished = false;

        if (revengeWaveActive)
        {
            revengeWaveActive = false;
            Debug.Log("Oleada de venganza completada");

            // Si la base enemiga está conquistada y terminó la oleada de venganza,
            // podemos detener las oleadas o continuar con algo más
            if (enemyBase != null && enemyBase.isConquered)
            {
                Debug.Log("¡Victoria completa! La base enemiga fue conquistada y la venganza fue derrotada.");
                return;
            }
        }
        else
        {
            currentWave++;
            Debug.Log($"Oleada {currentWave} completada. Preparando siguiente oleada...");
        }

        // Iniciar temporizador para siguiente oleada
        if (waitForAllEnemiesDead)
        {
            // Esperar un tiempo antes de revisar si iniciamos nueva oleada
            Invoke("CheckNextWave", 2f);
        }
        else
        {
            nextWaveTimer = timeBetweenWaves;
        }
    }

    void CheckNextWave()
    {
        // Si no quedan enemigos y no estamos en oleada de venganza
        if (activeEnemies.Count == 0 && !revengeWaveActive)
        {
            // Si la base no está conquistada, iniciar siguiente oleada
            if (enemyBase == null || !enemyBase.isConquered)
            {
                StartNextWave();
            }
        }
    }

    void IncreaseDifficulty()
    {
        timeBetweenWaves = Mathf.Max(10f, timeBetweenWaves - 5f);

        foreach (EnemyWaveConfig config in waveConfigs)
        {
            config.totalEnemies = Mathf.RoundToInt(config.totalEnemies * 1.5f);
            config.enemiesPerWave = Mathf.RoundToInt(config.enemiesPerWave * 1.2f);
        }

        Debug.Log("Dificultad aumentada");
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);

            if (revengeWaveActive)
            {
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    Transform player = FindNearestPlayer(enemy.transform.position);
                    if (player != null)
                    {
                        enemyAI.SetUsarPatrullaje(false);
                        enemyAI.AdicionarJogador(player);
                    }
                }
            }
        }
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);

            // Verificar si todos los enemigos han muerto
            if (activeEnemies.Count == 0 && allSpawnersFinished && waveInProgress)
            {
                WaveCompleted();
            }
        }
    }

    // Método para que los spawners notifiquen cuando terminen
    public void NotifySpawnerFinished()
    {
        // Verificar si todos los spawners han terminado
        bool allFinished = true;
        foreach (EnemySpawner spawner in enemySpawners)
        {
            if (spawner != null && spawner.IsSpawningActive())
            {
                allFinished = false;
                break;
            }
        }

        allSpawnersFinished = allFinished;

        // Si todos los spawners terminaron y no quedan enemigos, completar oleada
        if (allSpawnersFinished && activeEnemies.Count == 0 && waveInProgress)
        {
            WaveCompleted();
        }
    }

    public int GetActiveEnemiesCount()
    {
        return activeEnemies.Count;
    }

    public int GetCurrentWaveNumber()
    {
        return currentWave + 1;
    }

    public float GetTimeToNextWave()
    {
        return nextWaveTimer;
    }

    public bool IsRevengeWaveActive()
    {
        return revengeWaveActive;
    }

    public bool IsWaveInProgress()
    {
        return waveInProgress;
    }

    public void AddSpawner(EnemySpawner spawner)
    {
        if (!enemySpawners.Contains(spawner))
        {
            enemySpawners.Add(spawner);
        }
    }

    public void RemoveSpawner(EnemySpawner spawner)
    {
        if (enemySpawners.Contains(spawner))
        {
            enemySpawners.Remove(spawner);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}