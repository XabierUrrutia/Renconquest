using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum FactoryType
{
    Pequena,
    Mediana,
    Grande
}

public class EnemyBaseFactory : MonoBehaviour
{
    [Header("Configuración de Conquista")]
    public FactoryType factoryType;
    public float conquestTime = 10f;
    public float conquestRange = 5f;
    public Sprite conqueredSprite;

    [Header("Velocidades de Conquista")]
    public float growthSpeed = 2f;
    public float decaySpeed = 0.5f;

    [Header("Generación de Dinero")]
    public int moneyPerInterval = 25;
    public float moneyInterval = 1.2f;

    [Header("UI Elements")]
    public Slider conquestSlider;
    public Vector3 sliderOffset = new Vector3(0, 2f, 0);

    [Header("Configuración de Fog of War")]
    public float visionRadius = 15f;

    [Header("Spawn Configuration")]
    public bool enableSpawning = false;
    public GameObject enemyPrefab;
    public float spawnRadius = 3f;
    public float minSpawnDistance = 1.5f;

    [Tooltip("Tiempo que tarda la fábrica en empezar a sacar enemigos al inicio del juego.")]
    public float startDelay = 20f;

    // TIEMPOS
    public float spawnIntervalNormal = 8f;
    public float spawnIntervalDefensive = 4f;

    // LÍMITES
    public int maxConcurrentEnemiesNormal = 5;
    public int maxConcurrentEnemiesDefensive = 8;
    public int totalToSpawn = 0;

    [Header("Configuración de Pelotón")]
    public int minSquadSize = 2;
    public int maxSquadSize = 3;
    public float timeBetweenSquadMembers = 0.3f;

    [Header("Terrain Validation")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public LayerMask waterLayer;

    [Header("Estados")]
    public bool isConquered = false;
    public bool isGeneratingMoney = false;
    public bool isActiveSpawner = false;

    // Variables internas
    private float conquestProgress = 0f;
    private Canvas sliderCanvas;
    private List<GameObject> conqueringPlayers = new List<GameObject>();
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private Coroutine moneyGenerationCoroutine;
    private int spawnedCount = 0;

    private FogStaticVision fogStaticVision;
    private bool fogVisionInitialized = false;

    private Color neutralColor = Color.gray;
    private Color conqueredColor = Color.green;

    void Start()
    {
        InitializeFactory();
        SetValuesByType();

        if (spriteRenderer != null) spriteRenderer.color = neutralColor;
        InitializeFogVision();

        if (FactorySpawnManager.Instance != null)
        {
            FactorySpawnManager.Instance.RegisterFactory(this);
        }
    }

    void InitializeFactory()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null) collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = conquestRange;

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null) originalSprite = spriteRenderer.sprite;
    }

    private void InitializeFogVision()
    {
        fogStaticVision = GetComponent<FogStaticVision>();
        if (fogStaticVision == null) fogStaticVision = gameObject.AddComponent<FogStaticVision>();

        fogStaticVision.visionRadius = visionRadius;
        fogStaticVision.alwaysActive = true;
        fogStaticVision.enabled = false;
        fogVisionInitialized = true;
    }

    void SetValuesByType()
    {
        switch (factoryType)
        {
            case FactoryType.Pequena:
                conquestTime = 10f;
                moneyPerInterval = 50;
                maxConcurrentEnemiesNormal = 4;
                maxConcurrentEnemiesDefensive = 6;
                spawnIntervalNormal = 10f;
                spawnIntervalDefensive = 5f;
                visionRadius = 5f;
                minSquadSize = 2;
                maxSquadSize = 3;
                break;

            case FactoryType.Mediana:
                conquestTime = 12f;
                moneyPerInterval = 75;
                maxConcurrentEnemiesNormal = 6;
                maxConcurrentEnemiesDefensive = 10;
                spawnIntervalNormal = 8f;
                spawnIntervalDefensive = 4f;
                visionRadius = 7.5f;
                minSquadSize = 3;
                maxSquadSize = 4;
                break;

            case FactoryType.Grande:
                conquestTime = 15f;
                moneyPerInterval = 100;
                maxConcurrentEnemiesNormal = 10;
                maxConcurrentEnemiesDefensive = 15;
                spawnIntervalNormal = 6f;
                spawnIntervalDefensive = 3f;
                visionRadius = 10f;
                minSquadSize = 4;
                maxSquadSize = 6;
                break;
        }

        if (fogStaticVision != null) fogStaticVision.visionRadius = visionRadius;
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        if (!isConquered) UpdateConquestProgress();

        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
        {
            UpdateSliderPosition();
        }
    }

    void UpdateSliderPosition()
    {
        if (sliderCanvas != null)
        {
            sliderCanvas.transform.position = transform.position + sliderOffset;
            if (Camera.main != null) sliderCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    void UpdateConquestProgress()
    {
        if (conqueringPlayers.Count > 0)
        {
            float progressIncrement = (growthSpeed * conqueringPlayers.Count) / conquestTime;
            conquestProgress += progressIncrement * Time.deltaTime;
            conquestProgress = Mathf.Min(conquestProgress, conquestTime);

            if (conquestProgress > 0 && conquestSlider != null && !conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(true);
                UpdateSliderPosition();
            }
        }
        else if (conquestProgress > 0)
        {
            float progressDecrement = decaySpeed / conquestTime;
            conquestProgress -= progressDecrement * Time.deltaTime;
            conquestProgress = Mathf.Max(conquestProgress, 0);

            if (conquestProgress <= 0 && conquestSlider != null)
            {
                GameEvents.RaiseBuildingCaptureFailed();
                conquestSlider.gameObject.SetActive(false);
                spriteRenderer.color = neutralColor;
            }
        }

        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
            conquestSlider.value = conquestProgress / conquestTime;

        if (conquestProgress >= conquestTime && !isConquered) CompleteConquest();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || isConquered) return;
        if (other.CompareTag("Player") && !conqueringPlayers.Contains(other.gameObject))
        {
            if (conqueringPlayers.Count == 0)
                GameEvents.RaiseBuildingCaptureStarted();

            conqueringPlayers.Add(other.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        if (other.CompareTag("Player") && conqueringPlayers.Contains(other.gameObject))
            conqueringPlayers.Remove(other.gameObject);
    }

    private void CompleteConquest()
    {
        isConquered = true;

        GameEvents.RaiseBuildingCaptureCompleted();
        GameEvents.RaiseBuildingCaptured();

        if (spriteRenderer != null)
        {
            if (conqueredSprite != null)
            {
                spriteRenderer.sprite = conqueredSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = conqueredColor;
            }
        }

        if (conquestSlider != null) conquestSlider.gameObject.SetActive(false);
        conqueringPlayers.Clear();

        ActivateFogVision();
        StopSpawning();
        StartMoneyGeneration();
        GiveConquestReward();

        if (MoneyManager.Instance != null)
        {
            if (!MoneyManager.Instance.sePaganSalarios) MoneyManager.Instance.ActivarCobroDeSalarios();
            AplicarInflacionPorConquista();

            // --- REGISTRAR EN EL MANAGER PARA QUE SALGA EN LA UI (INGRESOS) ---
            MoneyManager.Instance.RegisterFactory(this);
        }
    }

    private void AplicarInflacionPorConquista()
    {
        if (MoneyManager.Instance == null) return;
        float subida = 0f;
        switch (factoryType)
        {
            case FactoryType.Mediana: subida = 0.2f; break;
            case FactoryType.Grande: subida = 0.5f; break;
        }
        if (subida > 0) MoneyManager.Instance.ModificarInflacion(subida);
    }

    private void ActivateFogVision()
    {
        if (!fogVisionInitialized) InitializeFogVision();
        if (fogStaticVision != null)
        {
            fogStaticVision.enabled = true;
            FogOfWar fog = FindObjectOfType<FogOfWar>();
            if (fog != null)
            {
                if (fogStaticVision.isInitialized)
                {
                    fog.UnregisterStaticVision(fogStaticVision);
                    fog.RegisterStaticVision(fogStaticVision);
                    fog.RequestUpdate();
                }
                else
                {
                    var method = fogStaticVision.GetType().GetMethod("InitializeFogSystem",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null) method.Invoke(fogStaticVision, null);
                }
            }
        }
    }

    private void StartMoneyGeneration()
    {
        if (!isGeneratingMoney)
        {
            isGeneratingMoney = true;
            moneyGenerationCoroutine = StartCoroutine(GenerateMoney());
        }
    }

    private IEnumerator GenerateMoney()
    {
        while (isConquered)
        {
            yield return new WaitForSeconds(moneyInterval);
            if (MoneyManager.Instance != null) MoneyManager.Instance.AddMoney(moneyPerInterval);
        }
    }

    private void GiveConquestReward()
    {
        int reward = 0;
        switch (factoryType)
        {
            case FactoryType.Pequena: reward = 100; break;
            case FactoryType.Mediana: reward = 200; break;
            case FactoryType.Grande: reward = 400; break;
        }
        if (MoneyManager.Instance != null) MoneyManager.Instance.AddMoney(reward);
    }

    public void TryStartSpawning()
    {
        if (isConquered || !enableSpawning || enemyPrefab == null) return;

        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnLoop());
            isActiveSpawner = true;
        }
    }

    private IEnumerator SpawnLoop()
    {
        Debug.Log($"[Factory {name}] Esperando {startDelay} segundos antes de empezar a spawnear...");
        yield return new WaitForSeconds(startDelay);

        while (!isConquered && enableSpawning)
        {
            bool isDefensiveMode = conqueringPlayers.Count > 0;
            float currentSpawnInterval = isDefensiveMode ? spawnIntervalDefensive : spawnIntervalNormal;
            int currentMaxEnemies = isDefensiveMode ? maxConcurrentEnemiesDefensive : maxConcurrentEnemiesNormal;

            CleanupNullSpawned();

            int slotsDisponibles = currentMaxEnemies - spawnedEnemies.Count;
            int enemigosRestantesParaTotal = (totalToSpawn > 0) ? (totalToSpawn - spawnedCount) : 999;
            int limiteReal = Mathf.Min(slotsDisponibles, enemigosRestantesParaTotal);

            if (limiteReal > 0)
            {
                int tamañoDeseado = Random.Range(minSquadSize, maxSquadSize + 1);
                int enemigosASpawnear = Mathf.Min(tamañoDeseado, limiteReal);

                for (int i = 0; i < enemigosASpawnear; i++)
                {
                    Vector3 spawnPosition = GetValidSpawnPosition();
                    if (spawnPosition != Vector3.zero)
                    {
                        SpawnEnemyAtPosition(spawnPosition);
                    }
                    else
                    {
                        break;
                    }
                    yield return new WaitForSeconds(timeBetweenSquadMembers);
                }
                yield return new WaitForSeconds(currentSpawnInterval);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }

        spawnCoroutine = null;
        isActiveSpawner = false;
    }

    private Vector3 GetValidSpawnPosition()
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector3 spawnPosition = GetRandomPositionAroundFactory();
            if (IsValidSpawnPosition(spawnPosition)) return spawnPosition;
        }
        return Vector3.zero;
    }

    private Vector3 GetRandomPositionAroundFactory()
    {
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minSpawnDistance, spawnRadius);
        Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * distance, Mathf.Sin(angle * Mathf.Deg2Rad) * distance);
        return transform.position + new Vector3(offset.x, offset.y, 0f);
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        if (!Physics2D.OverlapCircle(position, 0.5f, groundLayer)) return false;
        if (Physics2D.OverlapCircle(position, 0.5f, waterLayer)) return false;
        if (obstacleLayer != 0 && Physics2D.OverlapCircle(position, 0.5f, obstacleLayer)) return false;

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(position, 0.5f);
        foreach (Collider2D collider in nearbyEnemies)
        {
            if (collider.CompareTag("Enemy")) return false;
        }

        if (Vector3.Distance(position, transform.position) < 1f) return false;
        return true;
    }

    private void SpawnEnemyAtPosition(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.name = $"{enemyPrefab.name}_from_{name}_{spawnedCount}";
        spawnedEnemies.Add(enemy);
        spawnedCount++;

        var enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            if (enemyAI.baseJogador == null)
            {
                GameObject playerBase = GameObject.FindGameObjectWithTag("PlayerBase");
                if (playerBase != null) enemyAI.baseJogador = playerBase.transform;
            }
            enemyAI.SetUsarPatrullaje(true);
            if (EnemyManager.Instance != null) EnemyManager.Instance.RegistrarEnemy(enemyAI);
        }
    }

    private void CleanupNullSpawned()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null) spawnedEnemies.RemoveAt(i);
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        enableSpawning = false;
        isActiveSpawner = false;
    }

    public bool IsConquered() => isConquered;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isConquered ? Color.green : (conqueringPlayers.Count > 0 ? Color.yellow : Color.red);
        Gizmos.DrawWireSphere(transform.position, conquestRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }

    void OnDestroy()
    {
        if (sliderCanvas != null) Destroy(sliderCanvas.gameObject);
        if (fogStaticVision != null && fogStaticVision.isInitialized)
        {
            FogOfWar fog = FindObjectOfType<FogOfWar>();
            if (fog != null) fog.UnregisterStaticVision(fogStaticVision);
        }

        // --- LIMPIAR REFERENCIA EN MANAGER AL DESTRUIR ---
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.UnregisterFactory(this);
        }
    }

    public void ResetFactory()
    {
        isConquered = false;
        conquestProgress = 0f;

        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
        }
        spriteRenderer.color = neutralColor;

        if (fogStaticVision != null) fogStaticVision.enabled = false;
        isGeneratingMoney = false;
        if (moneyGenerationCoroutine != null) StopCoroutine(moneyGenerationCoroutine);
        conqueringPlayers.Clear();

        // --- DEJAR DE CONTAR INGRESOS SI SE RESETEA ---
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.UnregisterFactory(this);
        }
    }
}