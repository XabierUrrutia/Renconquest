using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public int maxHealth = 5;
    public Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Slider de Vida")]
    public Slider healthSlider;

    [Header("Health Bar Display")]
    public float showHealthBarTime = 3f;
    public bool alwaysShowHealthBar = false;

    private int currentHealth;
    private Camera mainCamera;
    private float healthBarTimer = 0f;
    private bool healthBarVisible = false;
    private Canvas healthBarCanvas;

    void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;

            healthSlider.transform.SetParent(transform);
            healthSlider.transform.localPosition = healthBarOffset;

            SetupWorldSpaceSlider();

            if (!alwaysShowHealthBar)
            {
                healthSlider.gameObject.SetActive(false);
                healthBarVisible = false;
            }
            else
            {
                healthBarVisible = true;
            }
        }
        else
        {
            Debug.LogError($"No hay Slider asignado para la barra de vida del enemigo: {name}");
        }
    }

    void SetupWorldSpaceSlider()
    {
        Canvas canvas = healthSlider.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("HealthBarCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = healthBarOffset;

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            healthSlider.transform.SetParent(canvasGO.transform);
            healthSlider.transform.localPosition = Vector3.zero;

            healthBarCanvas = canvas;
        }
        else
        {
            healthBarCanvas = canvas;
        }

        RectTransform sliderRT = healthSlider.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(150, 20);
        sliderRT.localScale = Vector3.one * 0.01f;
    }

    void Update()
    {
        if (healthSlider != null && mainCamera != null)
        {
            healthSlider.transform.rotation = mainCamera.transform.rotation;
        }

        if (healthSlider != null && healthBarVisible && !alwaysShowHealthBar)
        {
            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f)
            {
                HideHealthBar();
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (!alwaysShowHealthBar)
        {
            ShowHealthBar();
        }

        UpdateHealthBar();

        Debug.Log($"Enemy '{name}' recibiÛ {amount} de daÒo. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void ShowHealthBar()
    {
        if (healthSlider != null && !healthBarVisible)
        {
            healthSlider.gameObject.SetActive(true);
            healthBarVisible = true;
        }

        healthBarTimer = showHealthBarTime;
    }

    void HideHealthBar()
    {
        if (healthSlider != null && healthBarVisible)
        {
            healthSlider.gameObject.SetActive(false);
            healthBarVisible = false;
        }
    }

    void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthSlider.value = healthPercent;
        }
    }

    void Die()
    {
        // Notificar al EnemyWaveManager
        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.UnregisterEnemy(gameObject);
        }

        // Destruir la barra de vida
        if (healthSlider != null)
            Destroy(healthSlider.gameObject);

        if (healthBarCanvas != null)
            Destroy(healthBarCanvas.gameObject);

        if (SoundColector.Instance != null)
        {
            Vector3 pos = transform.position;

        bool isTankEnemy =
            GetComponentInParent<TankShooting>() != null ||
            GetComponentInParent<TankVisuals>() != null;

        if (isTankEnemy)
            SoundColector.Instance.PlayEnemyTankDeathAt(pos);
        else
            SoundColector.Instance.PlayEnemyInfantryDeathAt(pos);
        }

        Destroy(gameObject);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    public void SetHealthBarVisible(bool visible)
    {
        if (visible)
        {
            ShowHealthBar();
        }
        else
        {
            HideHealthBar();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        if (!alwaysShowHealthBar)
        {
            ShowHealthBar();
        }
    }
}