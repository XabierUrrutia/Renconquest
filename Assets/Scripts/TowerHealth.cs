using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))] // Necesario para cambiar la imagen
public class TowerHealth : MonoBehaviour, IHealth
{
    [Header("Configuración de Vida")]
    public int maxHealth = 100;

    [Header("UI Barra de Vida")]
    public Slider healthBar;

    [Header("Visuales de Daño")]
    public Sprite spriteIntacto;   // 100% a 66% vida
    public Sprite spriteDanyado;   // 66% a 33% vida
    public Sprite spriteMuyDanyado;// 33% a 0% vida

    [Header("Colores UI")]
    public Color healthyColor = Color.green;
    public Color criticalColor = Color.red;

    private int currentHealth;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;

    // Propiedades de IHealth
    public bool IsDead => isDead;
    public Transform Transform => transform;

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // 1. IMPORTANTE: Registrarse en el EnemyManager para que los enemigos sepan que existo
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegistrarNovoJogador(this.transform);
        }

        // 2. Registrar en GameManager (opcional, según tu arquitectura)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUnit(this);
        }

        // 3. Configurar UI Inicial
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            healthBar.interactable = false;
            UpdateHealthColor();
            // healthBar.gameObject.SetActive(false); // Descomentar si quieres ocultarla al inicio
        }

        // 4. Poner el sprite inicial
        UpdateDamageSprite();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        // Actualizar UI
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.value = currentHealth;
            UpdateHealthColor();
        }

        // Actualizar Sprite de la Torre
        UpdateDamageSprite();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        SoundColector.Instance?.PlayBuildingDestroyedAt(transform.position);

        // Avisar a los managers para que los enemigos dejen de atacar este punto
        if (EnemyManager.Instance != null) EnemyManager.Instance.RemoverJogador(this.transform);
        if (GameManager.Instance != null) GameManager.Instance.UnregisterUnit(this);

        Destroy(gameObject);
    }

    // --- Lógica Visual de Sprites ---
    void UpdateDamageSprite()
    {
        if (spriteRenderer == null) return;

        float porcentaje = (float)currentHealth / maxHealth;

        if (porcentaje > 0.66f) // Más del 66% de vida
        {
            if (spriteIntacto != null) spriteRenderer.sprite = spriteIntacto;
        }
        else if (porcentaje > 0.33f) // Entre 33% y 66%
        {
            if (spriteDanyado != null) spriteRenderer.sprite = spriteDanyado;
        }
        else // Menos del 33% (Crítico)
        {
            if (spriteMuyDanyado != null) spriteRenderer.sprite = spriteMuyDanyado;
        }
    }

    // --- Métodos de Interfaz IHealth ---
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsFullHealth() => currentHealth >= maxHealth;

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            UpdateHealthColor();
        }
        UpdateDamageSprite(); // Actualizar sprite al curar también
    }

    public void SetHealthBarVisible(bool visible)
    {
        if (healthBar != null) healthBar.gameObject.SetActive(visible);
    }

    public int GetCurrentShield() => 0;
    public int GetMaxShield() => 0;
    public bool IsFullShield() => true;
    public void RepairShield(int amount) { }

    void UpdateHealthColor()
    {
        if (healthBar == null) return;
        Image fillImage = healthBar.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            fillImage.color = Color.Lerp(criticalColor, healthyColor, healthPercent);
        }
    }

    void OnDestroy()
    {
        if (EnemyManager.Instance != null) EnemyManager.Instance.RemoverJogador(this.transform);
    }
}