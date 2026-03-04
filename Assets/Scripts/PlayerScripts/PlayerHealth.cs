// PlayerHealth.cs
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IHealth
{
    [Header("Health Settings")]
    public int maxHealth = 4;
    private int currentHealth;

    [Header("UI")]
    public Slider healthBar;
    public Vector3 healthBarOffset = new Vector3(0, 1f, 0);

    [Header("Health Bar Display")]
    public float showHealthBarTime = 3f;
    private float healthBarTimer = 0f;
    private bool healthBarVisible = false;

    [Header("Death Settings")]
    public float deathDelay = 1.5f;
    public bool disableMovementOnDeath = true;

    private bool isDead = false;
    private bool isRegistered = false;

    // Implementación de propiedades de IHealth
    public bool IsDead => isDead;
    public Transform Transform => transform;

    void Start()
    {
        currentHealth = maxHealth;

        // Registrar este jugador en el GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUnit(this);
            isRegistered = true;
        }

        // Ocultar la barra de vida al inicio
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            UpdateHealthBarPosition();
        }
    }

    void Update()
    {
        // Actualizar posición de la barra si está visible
        if (healthBar != null && healthBarVisible && !isDead)
        {
            UpdateHealthBarPosition();

            // Contar el tiempo y ocultar la barra si ha pasado el tiempo
            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f && healthBarVisible)
            {
                HideHealthBar();
            }
        }
    }

    void UpdateHealthBarPosition()
    {
        if (Camera.main != null)
        {
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
        }
    }

    // Implementación de IHealth.TakeDamage
    public void TakeDamage(int damage)
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Mostrar la barra de vida cuando recibe daño
        ShowHealthBar();
        UpdateHealthBar();
        GameEvents.RaiseUnitUnderAttack();


        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // MÉTODOS PARA MOSTRAR/OCULTAR LA BARRA DE VIDA
    void ShowHealthBar()
    {
        if (healthBar != null && !healthBarVisible)
        {
            healthBar.gameObject.SetActive(true);
            healthBarVisible = true;
        }

        // Reiniciar el temporizador
        healthBarTimer = showHealthBarTime;
    }

    void HideHealthBar()
    {
        if (healthBar != null && healthBarVisible)
        {
            healthBar.gameObject.SetActive(false);
            healthBarVisible = false;
        }
    }

    // Implementación de IHealth.Heal
    public void Heal(int amount)
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        Debug.Log($"Curado: +{amount}. Vida actual: {currentHealth}/{maxHealth}");
    }

    // Implementación de IHealth.GetCurrentHealth
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Implementación de IHealth.GetMaxHealth
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    // Implementación de IHealth.IsFullHealth
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    // Implementación de métodos de escudo (opcional - lanzan excepción si se llaman)
    public int GetCurrentShield()
    {
        // Los soldados normales no tienen escudo
        return 0;
    }

    public int GetMaxShield()
    {
        // Los soldados normales no tienen escudo
        return 0;
    }

    public bool IsFullShield()
    {
        // Los soldados normales no tienen escudo
        return true;
    }

    public void RepairShield(int amount)
    {
        // Los soldados normales no tienen escudo
        Debug.LogWarning("RepairShield llamado en un soldado sin escudo");
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    // Implementación de IHealth.Die
    public void Die()
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        isDead = true;
        Debug.Log("Soldado muerto!");

        // 🔊 SONIDO DE MUERTE
        if (SoundColector.Instance != null)
        {
            // Si tiene tag "Tank" → sonido de muerte de tanque

            Vector3 pos = transform.position;

        bool isTankEnemy =
            GetComponentInParent<TankShooting>() != null ||
            GetComponentInParent<TankVisuals>() != null;

        if (isTankEnemy)
            SoundColector.Instance.PlayEnemyTankDeathAt(pos);
        else
            SoundColector.Instance.PlayEnemyInfantryDeathAt(pos);
        }

        // Notificar al GameManager que esta unidad ha muerto
        if (isRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUnit(this);
        }

        // Notificar al EnemyManager que este jugador ha muerto
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RemoverJogador(transform);
        }

        // Resto del código existente...
        HideHealthBar();

        if (disableMovementOnDeath)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;
        }

        Invoke("DeactivatePlayer", deathDelay);
    }

    void DeactivatePlayer()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // Asegurarse de desregistrar si el objeto es destruido
        if (isRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUnit(this);
        }
    }

    // Implementación de IHealth.SetHealthBarVisible
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

    // Método para revivir al jugador
    public void Revive()
    {
        if (isDead)
        {
            isDead = false;
            currentHealth = maxHealth;
            gameObject.SetActive(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterUnit(this);
                isRegistered = true;
            }

            // Reactivar componentes si es necesario
            if (disableMovementOnDeath)
            {
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }

                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                    collider.enabled = true;
            }

            UpdateHealthBar();
        }
    }
}