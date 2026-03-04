using UnityEngine;
using System.Collections;

/// <summary>
/// Spawner de inimigos com opção para só ativar quando o jogador se aproximar.
/// Agora suporta número de inimigos configurável por spawner (local) ou global.
/// Mantive a lógica de waves / spawn all-at-once existente e adicionei checagem de proximidade.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab & quantidade")]
    public GameObject enemyPrefab;

    [Tooltip("Se true usa o valor local (localEnemyCount). Se false usa enemyCount (global).")]
    public bool useLocalEnemyCount = true;
    [Tooltip("Número de inimigos a spawnar neste spawner (quando useLocalEnemyCount = true)")]
    public int localEnemyCount = 5;

    [Tooltip("Número de inimigos (global) usado se useLocalEnemyCount = false")]
    public int enemyCount = 10;

    [Header("Configuración de Spawn alrededor de la Base")]
    public float spawnRadius = 5f;
    public float minDistanceFromBase = 1f;
    public bool spawnInCircle = true;

    [Header("Opciones de Terreno")]
    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public LayerMask obstacleLayer;

    [Header("Spawning por Grupos")]
    public bool spawnInWaves = false;
    public int enemiesPerWave = 3;
    public float timeBetweenWaves = 2f;

    [Header("Activación por proximidad del jugador")]
    [Tooltip("Se true, el spawner esperará a que un jugador esté cerca (playerActivationRadius) antes de empezar a spawnear.")]
    public bool requirePlayerProximity = true;
    [Tooltip("Radio alrededor del spawner en el que el jugador debe estar para activar el spawn.")]
    public float playerActivationRadius = 20f;
    [Tooltip("Intervalo (s) entre comprobaciones de proximidad mientras espera.")]
    public float proximityCheckInterval = 1f;

    // runtime
    private int enemiesSpawned = 0;
    private bool spawningActive = false;
    private Coroutine spawnCoroutine;
    private int currentWaveNumber = 0;

    // total a spawnar neste ciclo (calculado em StartSpawning)
    private int totalToSpawn = 0;

    void Start()
    {
        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.AddSpawner(this);
        }
    }

    public void StartSpawning()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefab não atribuído em " + name);
            return;
        }

        // calcula totalToSpawn com base na opção local/global
        totalToSpawn = useLocalEnemyCount ? Mathf.Max(0, localEnemyCount) : Mathf.Max(0, enemyCount);

        // Reiniciar estado
        spawningActive = true;
        enemiesSpawned = 0;
        currentWaveNumber++;

        if (totalToSpawn <= 0)
        {
            Debug.LogWarning($"EnemySpawner '{name}': totalToSpawn é 0 — nada será spawnado.");
            SpawningFinished();
            return;
        }

        // Se se exige proximidade do jogador, espera até que haja um jogador perto
        if (requirePlayerProximity)
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(WaitForPlayerThenSpawn());
        }
        else
        {
            // Inicia imediatamente
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);

            if (spawnInWaves)
                spawnCoroutine = StartCoroutine(SpawnWaves());
            else
                spawnCoroutine = StartCoroutine(SpawnAllAtOnce());
        }
    }

    IEnumerator WaitForPlayerThenSpawn()
    {
        Debug.Log($"Spawner '{name}': aguardando jogador a {playerActivationRadius} unidades para iniciar spawn...");
        // espera até que algum jogador esteja dentro do raio
        while (spawningActive && !IsAnyPlayerInRange(transform.position, playerActivationRadius))
        {
            yield return new WaitForSeconds(proximityCheckInterval);
        }

        if (!spawningActive)
        {
            spawnCoroutine = null;
            yield break;
        }

        Debug.Log($"Spawner '{name}': jogador detectado — iniciando spawn (waves={spawnInWaves})");

        if (spawnInWaves)
            spawnCoroutine = StartCoroutine(SpawnWaves());
        else
            spawnCoroutine = StartCoroutine(SpawnAllAtOnce());
    }

    IEnumerator SpawnAllAtOnce()
    {
        int spawned = 0;
        int tries = 0;
        int maxTries = totalToSpawn * 10;

        while (spawned < totalToSpawn && tries < maxTries && spawningActive)
        {
            Vector3 spawnPosition = GetSpawnPositionAroundBase();

            if (IsValidSpawnPosition(spawnPosition))
            {
                GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                RegisterEnemyWithSystems(enemy);
                spawned++;
                enemiesSpawned++;
            }

            tries++;

            // pequeno delay para não sobrecarregar frame
            if (spawned % 5 == 0)
                yield return null;
        }

        Debug.Log($"Spawner '{name}': Gerados {spawned} inimigos (total esperado: {totalToSpawn})");

        SpawningFinished();
    }

    public IEnumerator SpawnWaves()
    {
        int waveNumber = 1;

        while (enemiesSpawned < totalToSpawn && spawningActive)
        {
            Debug.Log($"Spawner '{name}': Iniciando ola {waveNumber} (restantes: {totalToSpawn - enemiesSpawned})");

            int enemiesThisWave = Mathf.Min(enemiesPerWave, totalToSpawn - enemiesSpawned);
            int spawnedThisWave = 0;
            int tries = 0;
            int maxTries = enemiesThisWave * 10;

            while (spawnedThisWave < enemiesThisWave && tries < maxTries && spawningActive)
            {
                Vector3 spawnPosition = GetSpawnPositionAroundBase();

                if (IsValidSpawnPosition(spawnPosition))
                {
                    GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                    RegisterEnemyWithSystems(enemy);
                    spawnedThisWave++;
                    enemiesSpawned++;
                }

                tries++;
            }

            Debug.Log($"Spawner '{name}': Ola {waveNumber} completada - {spawnedThisWave} inimigos. Total: {enemiesSpawned}/{totalToSpawn}");

            waveNumber++;

            if (enemiesSpawned < totalToSpawn && spawningActive)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        Debug.Log($"Spawner '{name}': Todas as olas completadas. Total gerado: {enemiesSpawned}");

        SpawningFinished();
    }

    void SpawningFinished()
    {
        spawningActive = false;
        spawnCoroutine = null;

        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.NotifySpawnerFinished();
        }
    }

    public void StopSpawning()
    {
        spawningActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        Debug.Log($"Spawner '{name}': Spawning parado. Gerados {enemiesSpawned}/{totalToSpawn}");
    }

    void RegisterEnemyWithSystems(GameObject enemy)
    {
        if (EnemyManager.Instance != null)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                EnemyManager.Instance.RegistrarEnemy(enemyAI);
            }
        }

        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.RegisterEnemy(enemy);
        }

        if (EnemyWaveManager.Instance != null && EnemyWaveManager.Instance.IsRevengeWaveActive())
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.SetUsarPatrullaje(false);
            }
        }
    }

    Vector3 GetSpawnPositionAroundBase()
    {
        Vector3 basePosition = transform.position;

        if (spawnInCircle)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(minDistanceFromBase, spawnRadius);

            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );

            return basePosition + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            float x = Random.Range(-spawnRadius, spawnRadius);
            float y = Random.Range(-spawnRadius, spawnRadius);

            if (Mathf.Abs(x) < minDistanceFromBase) x = Mathf.Sign(x) * minDistanceFromBase;
            if (Mathf.Abs(y) < minDistanceFromBase) y = Mathf.Sign(y) * minDistanceFromBase;

            return basePosition + new Vector3(x, y, 0f);
        }
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        if (!Physics2D.OverlapCircle(position, 0.3f, groundLayer))
        {
            return false;
        }

        if (Physics2D.OverlapCircle(position, 0.3f, waterLayer))
        {
            return false;
        }

        if (obstacleLayer != 0 && Physics2D.OverlapCircle(position, 0.5f, obstacleLayer))
        {
            return false;
        }

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(position, 1f);
        foreach (Collider2D collider in nearbyEnemies)
        {
            if (collider.CompareTag("Enemy"))
            {
                return false;
            }
        }

        return true;
    }

    // ---------- Helper: verifica se qualquer jogador está dentro do raio dado ------------
    bool IsAnyPlayerInRange(Vector3 worldPos, float radius)
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0) return false;

        float r2 = radius * radius;
        foreach (var p in players)
        {
            if (p == null) continue;
            Vector2 d = p.transform.position - worldPos;
            if (d.sqrMagnitude <= r2) return true;
        }
        return false;
    }

    public void SetWaveParameters(int count, int perWave, float betweenWaves)
    {
        enemyCount = count;
        enemiesPerWave = perWave;
        timeBetweenWaves = betweenWaves;
    }

    public void ResetSpawner()
    {
        enemiesSpawned = 0;
        spawningActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public bool IsSpawningActive()
    {
        return spawningActive;
    }

    public int GetEnemiesSpawned()
    {
        return enemiesSpawned;
    }

    public int GetTotalEnemiesToSpawn()
    {
        return totalToSpawn;
    }

    public float GetSpawnProgress()
    {
        if (totalToSpawn == 0) return 1f;
        return (float)enemiesSpawned / totalToSpawn;
    }

    void OnDestroy()
    {
        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.RemoveSpawner(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (spawnInCircle)
        {
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            if (minDistanceFromBase > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, minDistanceFromBase);
            }
        }
        else
        {
            Vector3 size = new Vector3(spawnRadius * 2, spawnRadius * 2, 0.1f);
            Gizmos.DrawWireCube(transform.position, size);
        }

        // desenha raio de ativação do jogador
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, playerActivationRadius);
    }
}