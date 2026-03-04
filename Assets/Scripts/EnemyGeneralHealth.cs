using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class EnemyGeneralHealth : MonoBehaviour, IHealth
{
    [Header("Configuración de Vida")]
    public int maxHealth = 500;
    private int currentHealth;

    [Header("Configuración de Escudo")]
    public bool hasShield = true;
    public int maxShield = 200;
    private int currentShield;

    [Header("Regeneración de Escudo")]
    public float shieldRegenRate = 5f;
    public float shieldRegenDelay = 5f;
    private float lastDamageTime = 0f;

    [Header("Configuración Visual (UI Flotante)")]
    public Vector3 uiOffset = new Vector3(0f, 2.5f, 0f);

    // --- NUEVO: SEPARACIÓN ENTRE BARRAS ---
    [Tooltip("Distancia vertical entre la barra de vida y la de escudo")]
    public float barSeparation = 0.5f;
    // -------------------------------------

    public float showBarsTime = 3f;
    public bool alwaysShowBars = false;

    [Header("Orden de Dibujado (Sorting)")]
    public string canvasSortingLayer = "UI";
    public int canvasSortingOrder = 100;

    [Header("Sliders (Arrastrar Prefabs)")]
    public Slider healthSlider;
    public Slider shieldSlider;

    // Variables internas
    private Camera mainCamera;
    private float barsTimer = 0f;
    private bool barsVisible = false;
    private Canvas generatedCanvas;
    private bool isDead = false;

    // Propiedades IHealth
    public bool IsDead => isDead;
    public Transform Transform => transform;

    void Awake()
    {
        currentHealth = maxHealth;
        currentShield = maxShield;
    }

    void Start()
    {
        mainCamera = Camera.main;
        SetupWorldSpaceUI();
        UpdateUI();
        if (EnemyWaveManager.Instance != null) EnemyWaveManager.Instance.RegisterEnemy(gameObject);
    }

    void SetupWorldSpaceUI()
    {
        if (healthSlider == null && shieldSlider == null) return;

        // 1. Crear Canvas Contenedor
        GameObject canvasGO = new GameObject("GeneralUI_AutoCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = uiOffset;

        generatedCanvas = canvasGO.AddComponent<Canvas>();
        generatedCanvas.renderMode = RenderMode.WorldSpace;

        // Configurar capa para que se vea encima del césped
        generatedCanvas.overrideSorting = true;
        generatedCanvas.sortingLayerName = canvasSortingLayer;
        generatedCanvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        // 2. Colocar BARRA DE VIDA (Abajo) -> Posición Y = 0
        if (healthSlider != null)
        {
            PrepareSlider(healthSlider, canvasGO.transform, 0f);
        }

        // 3. Colocar BARRA DE ESCUDO (Arriba) -> Posición Y = barSeparation
        // Al poner un valor positivo, la subimos por encima de la vida
        if (shieldSlider != null && hasShield)
        {
            PrepareSlider(shieldSlider, canvasGO.transform, barSeparation);
        }

        if (!alwaysShowBars) SetBarsActive(false);
    }

    void PrepareSlider(Slider slider, Transform parentCanvas, float yPosAdjustment)
    {
        slider.transform.SetParent(parentCanvas);

        // Aquí aplicamos la altura (yPosAdjustment)
        slider.transform.localPosition = new Vector3(0, yPosAdjustment, 0);

        slider.transform.localRotation = Quaternion.identity;
        slider.transform.localScale = Vector3.one;

        // Ajustar tamaño visual
        RectTransform rt = slider.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(150, 20); // Tamaño base
            rt.localScale = Vector3.one * 0.01f; // Escala reducida para mundo
        }
    }

    void Update()
    {
        if (isDead) return;

        // Regeneración Escudo
        if (hasShield && currentShield < maxShield)
        {
            if (Time.time - lastDamageTime >= shieldRegenDelay) RegenerateShield();
        }

        // Billboard (Mirar a cámara)
        if (generatedCanvas != null && mainCamera != null)
        {
            generatedCanvas.transform.rotation = mainCamera.transform.rotation;
        }

        // Ocultar barras
        if (barsVisible && !alwaysShowBars)
        {
            barsTimer -= Time.deltaTime;
            if (barsTimer <= 0f) HideBars();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        lastDamageTime = Time.time;
        int damageRemaining = amount;

        // Daño primero al escudo
        if (hasShield && currentShield > 0)
        {
            if (currentShield >= damageRemaining)
            {
                currentShield -= damageRemaining;
                damageRemaining = 0;
            }
            else
            {
                damageRemaining -= currentShield;
                currentShield = 0;
            }
        }

        // Daño restante a la vida
        if (damageRemaining > 0)
        {
            currentHealth -= damageRemaining;
            currentHealth = Mathf.Max(0, currentHealth);
        }

        if (!alwaysShowBars) ShowBars();
        UpdateUI();
        if (currentHealth <= 0) Die();
    }

    void UpdateUI()
    {
        if (healthSlider != null) healthSlider.value = (float)currentHealth / maxHealth;

        if (shieldSlider != null)
        {
            // Si el escudo llega a 0, ocultamos la barra azul completamente
            shieldSlider.gameObject.SetActive(currentShield > 0);
            shieldSlider.value = (float)currentShield / maxShield;
        }
    }

    void ShowBars() { if (!barsVisible) { SetBarsActive(true); barsVisible = true; } barsTimer = showBarsTime; }
    void HideBars() { if (barsVisible) { SetBarsActive(false); barsVisible = false; } }
    void SetBarsActive(bool active) { if (generatedCanvas != null) generatedCanvas.enabled = active; }

    void RegenerateShield()
    {
        float amountToAdd = shieldRegenRate * Time.deltaTime;
        if (amountToAdd >= 1f) { currentShield += Mathf.FloorToInt(amountToAdd); currentShield = Mathf.Min(currentShield, maxShield); UpdateUI(); }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        if (EnemyWaveManager.Instance != null) EnemyWaveManager.Instance.UnregisterEnemy(gameObject);
        SoundColector.Instance?.PlayBuildingDestroyedAt(transform.position);
        Destroy(gameObject);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsFullHealth() => currentHealth >= maxHealth;
    public void Heal(int amount) { currentHealth += amount; UpdateUI(); }
    public void SetHealthBarVisible(bool visible) { if (visible) ShowBars(); else HideBars(); }
    public int GetCurrentShield() => currentShield;
    public int GetMaxShield() => maxShield;
    public bool IsFullShield() => currentShield >= maxShield;
    public void RepairShield(int amount) { currentShield += amount; UpdateUI(); }
}