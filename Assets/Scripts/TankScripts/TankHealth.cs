using UnityEngine;
using UnityEngine.UI;

// Añadimos IHealth para que funcione con tu HUDManager y el sistema de selección
public class TankHealth : MonoBehaviour, IHealth
{
    [Header("Resistencia")]
    public int maxHealth = 1200;
    private int currentHealth;

    [Header("Efectos Visuales")]
    public GameObject explosionPrefab; // El prefab de explosión que ya tenías

    [Header("UI - Barra de Vida Flotante")]
    public Slider healthBar; // ARRASTRA AQUÍ TU SLIDER
    // Offset más alto porque el tanque es más grande que el soldado
    public Vector3 healthBarOffset = new Vector3(0, 2.5f, 0);
    public float showHealthBarTime = 3f;

    private float healthBarTimer = 0f;
    private bool healthBarVisible = false;

    private bool isDead = false;
    private bool isRegistered = false;

    // Propiedades de la interfaz IHealth
    public bool IsDead => isDead;
    public Transform Transform => transform;

    void Start()
    {
        currentHealth = maxHealth;

        // Registrar en GameManager (si existe)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUnit(this);
            isRegistered = true;
        }

        // Configuración inicial de la barra
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            healthBar.maxValue = 1f;
            healthBar.value = 1f;
            UpdateHealthBarPosition();
        }
    }

    void Update()
    {
        // Lógica para que la barra de vida siga al tanque y desaparezca sola
        if (healthBar != null && healthBarVisible && !isDead)
        {
            UpdateHealthBarPosition();

            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f)
            {
                HideHealthBar();
            }
        }
    }

    void UpdateHealthBarPosition()
    {
        if (Camera.main != null && healthBar != null)
        {
            // Convierte la posición 3D del tanque a posición 2D de la pantalla
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
        }
    }

    // --- SISTEMA DE DAÑO ---

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Mostrar barra al recibir daño
        ShowHealthBar();
        UpdateHealthBarValue();

        // Opcional: Sonido de impacto metálico aquí si quisieras

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBarValue();
        Debug.Log($"Tanque reparado: +{amount}");
    }

    // --- MUERTE DEL TANQUE ---

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. SFX morte do tanque (SFX-TankDeathClips)
        if (SoundColector.Instance != null)
        {
            SoundColector.Instance.PlayTankDeathAt(transform.position);
        }

        // 2. Efecto visual (Tu explosión original)
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 3. Desregistrar de los Managers
        if (isRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUnit(this);
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RemoverJogador(transform);
        }

        // 4. Ocultar barra de vida inmediatamente
        if (healthBar != null) Destroy(healthBar.gameObject);

        // 5. Destruir el tanque
        Destroy(gameObject);
    }

    // --- MÉTODOS UI ---

    void ShowHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBarVisible = true;
            healthBarTimer = showHealthBarTime; // Reiniciar contador
        }
    }

    void HideHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            healthBarVisible = false;
        }
    }

    void UpdateHealthBarValue()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    // --- IMPLEMENTACIÓN INTERFAZ IHEALTH (Getters necesarios) ---

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsFullHealth() => currentHealth >= maxHealth;

    public void SetHealthBarVisible(bool visible)
    {
        if (visible) ShowHealthBar();
        else HideHealthBar();
    }

    // El tanque podría tener escudo en el futuro, pero por ahora devolvemos 0/dummy
    public int GetCurrentShield() => 0;
    public int GetMaxShield() => 0;
    public bool IsFullShield() => true;
    public void RepairShield(int amount) { }

    void OnDestroy()
    {
        // Limpieza de seguridad
        if (isRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUnit(this);
        }

        // Si destruimos el tanque, asegurarnos de borrar su barra de vida si quedaba por ahí
        if (healthBar != null) Destroy(healthBar.gameObject);
    }
}