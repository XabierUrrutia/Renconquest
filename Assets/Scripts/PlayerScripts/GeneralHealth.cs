using UnityEngine;
using UnityEngine.UI;

public class GeneralHealth : MonoBehaviour, IHealth
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Shield Settings (For Generals)")]
    public bool hasShield = true; // IMPORTANTE: Activar solo para generales
    public int maxShield = 50;
    private int currentShield;
    public float shieldRegenRate = 1f; // Escudos por segundo
    public float shieldRegenDelay = 3f; // Segundos despu�s del da�o para regenerar
    private float shieldRegenTimer = 0f;
    private bool isShieldBroken = false;

    [Header("UI")]
    public Slider healthBar;
    public Slider shieldBar; // Nueva barra para escudo
    public Vector3 healthBarOffset = new Vector3(0, 1f, 0);
    public GameObject generalIndicator; // Indicador visual para generales

    [Header("Health Bar Display")]
    public float showHealthBarTime = 3f;
    private float healthBarTimer = 0f;
    private bool healthBarVisible = false;

    [Header("Death Settings")]
    public float deathDelay = 1.5f;
    public bool disableMovementOnDeath = true;

    private bool isDead = false;
    private bool isRegistered = false;

    // Implementaci�n de propiedades de IHealth
    public bool IsDead => isDead;
    public Transform Transform => transform;

    void Start()
    {
        currentHealth = maxHealth;

        // Inicializar escudo si tiene
        if (hasShield)
        {
            currentShield = maxShield;
            isShieldBroken = false;

            Debug.Log($"[GeneralHealth] {name}: Escudo inicializado: {currentShield}/{maxShield}");

            // Activar indicador visual para generales
            if (generalIndicator != null)
                generalIndicator.SetActive(true);
        }

        // Registrar esta unidad en el GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUnit(this);
            isRegistered = true;
            Debug.Log($"[GeneralHealth] {name}: Registrado en GameManager");
        }

        // Configurar UI
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            UpdateHealthBarPosition();
        }

        if (shieldBar != null)
        {
            shieldBar.gameObject.SetActive(false);
            UpdateShieldBarPosition();
        }

        // Configurar colores
        if (shieldBar != null && hasShield)
        {
            Image fillImage = shieldBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = new Color(0.2f, 0.6f, 1f, 1f); // Azul para escudo
        }
    }

    void Update()
    {
        // Actualizar posici�n de las barras si est�n visibles
        if (!isDead)
        {
            if (healthBarVisible)
            {
                UpdateHealthBarPosition();
                if (hasShield && shieldBar != null)
                    UpdateShieldBarPosition();

                // Contar el tiempo y ocultar las barras si ha pasado
                healthBarTimer -= Time.deltaTime;
                if (healthBarTimer <= 0f && healthBarVisible)
                {
                    HideHealthBars();
                }
            }

            // Regenerar escudo si corresponde
            if (hasShield && !isDead)
            {
                RegenerateShield();
            }
        }
    }

    void UpdateHealthBarPosition()
    {
        if (Camera.main != null && healthBar != null)
        {
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
        }
    }

    void UpdateShieldBarPosition()
    {
        if (Camera.main != null && shieldBar != null)
        {
            // Posicionar la barra de escudo un poco m�s arriba que la de salud
            shieldBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset + new Vector3(0, 0, 0));
        }
    }

    // Implementaci�n de IHealth.TakeDamage - VERSI�N CORREGIDA
    public void TakeDamage(int damage)
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        Debug.Log($"[GeneralHealth] {name}: Recibiendo {damage} de da�o. Escudo: {currentShield}/{maxShield}, Vida: {currentHealth}/{maxHealth}");

        int remainingDamage = damage;

        // Primero da�ar el escudo si existe y no est� roto
        if (hasShield && currentShield > 0 && !isShieldBroken)
        {
            Debug.Log($"[GeneralHealth] {name}: Escudo absorbiendo da�o. Escudo antes: {currentShield}");

            // El escudo absorbe el da�o
            int shieldDamage = Mathf.Min(currentShield, remainingDamage);
            currentShield -= shieldDamage;
            remainingDamage -= shieldDamage;

            Debug.Log($"[GeneralHealth] {name}: Escudo da�ado: {shieldDamage}. Escudo despu�s: {currentShield}, Da�o restante: {remainingDamage}");

            // Si el escudo lleg� a 0, marcarlo como roto
            if (currentShield <= 0)
            {
                isShieldBroken = true;
                currentShield = 0;

                Debug.Log($"[GeneralHealth] {name}: �Escudo roto!");
            }

            shieldRegenTimer = shieldRegenDelay; // Reiniciar temporizador de regeneraci�n
        }
        else if (hasShield && (currentShield <= 0 || isShieldBroken))
        {
            Debug.Log($"[GeneralHealth] {name}: Escudo ya est� roto o agotado, da�o va directo a vida");
        }

        // Aplicar da�o restante a la salud
        if (remainingDamage > 0)
        {
            Debug.Log($"[GeneralHealth] {name}: Aplicando {remainingDamage} a la salud. Vida antes: {currentHealth}");
            currentHealth -= remainingDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // Si recibi� da�o directo a la vida, reiniciar regeneraci�n de escudo
            if (hasShield)
            {
                shieldRegenTimer = shieldRegenDelay;
            }
        }

        // Mostrar las barras de vida/escudo
        ShowHealthBars();
        UpdateHealthBar();
        UpdateShieldBar();

        // Debug para ver estado actual
        Debug.Log($"[GeneralHealth] {name}: Estado despu�s del da�o - Escudo: {currentShield}/{maxShield}, Vida: {currentHealth}/{maxHealth}, Escudo roto: {isShieldBroken}");

        GameEvents.RaiseUnitUnderAttack();


        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // M�TODOS PARA MOSTRAR/OCULTAR LAS BARRAS
    void ShowHealthBars()
    {
        // Solo activamos cosas si tenemos la barra de vida asignada
        if (healthBar != null)
        {
            // Activamos la barra de vida siempre al recibir da�o
            if (!healthBarVisible)
            {
                healthBar.gameObject.SetActive(true);
                healthBarVisible = true;
            }

            // CONTROL DEL ESCUDO:
            // Solo activamos la barra de escudo SI tiene escudo Y es mayor que 0
            if (hasShield && shieldBar != null && currentShield > 0)
            {
                shieldBar.gameObject.SetActive(true);
            }
            else if (shieldBar != null)
            {
                // Si el da�o rompi� el escudo, nos aseguramos de que est� apagada
                shieldBar.gameObject.SetActive(false);
            }
        }

        // Reiniciamos el contador para que se oculten despu�s de un tiempo
        healthBarTimer = showHealthBarTime;
    }

    void HideHealthBars()
    {
        if (healthBar != null && healthBarVisible)
        {
            healthBar.gameObject.SetActive(false);

            if (hasShield && shieldBar != null)
                shieldBar.gameObject.SetActive(false);

            healthBarVisible = false;
        }
    }

    // REGENERACI�N DE ESCUDO
    void RegenerateShield()
    {
        if (isShieldBroken || currentShield < maxShield)
        {
            shieldRegenTimer -= Time.deltaTime;

            if (shieldRegenTimer <= 0f)
            {
                float regenAmount = shieldRegenRate * Time.deltaTime;
                currentShield = Mathf.Min(maxShield, currentShield + Mathf.RoundToInt(regenAmount));

                Debug.Log($"[GeneralHealth] {name}: Regenerando escudo. Nuevo valor: {currentShield}/{maxShield}");

                if (currentShield >= maxShield)
                {
                    isShieldBroken = false;
                    currentShield = maxShield;
                    Debug.Log($"[GeneralHealth] {name}: Escudo completamente regenerado");
                }

                UpdateShieldBar();
            }
        }
    }

    // Implementaci�n de IHealth.Heal
    public void Heal(int amount)
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        Debug.Log($"[GeneralHealth] {name}: Curado +{amount}. Vida: {currentHealth}/{maxHealth}");
    }

    // Implementaci�n de IHealth.RepairShield
    public void RepairShield(int amount)
    {
        if (isDead || !hasShield || GameManager.Instance.IsGameOver()) return;

        currentShield += amount;
        currentShield = Mathf.Clamp(currentShield, 0, maxShield);
        isShieldBroken = false;

        UpdateShieldBar();
        Debug.Log($"[GeneralHealth] {name}: Escudo reparado +{amount}. Escudo: {currentShield}/{maxShield}");
    }

    // Implementaci�n de IHealth.GetCurrentHealth
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Implementaci�n de IHealth.GetMaxHealth
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    // Implementaci�n de IHealth.GetCurrentShield
    public int GetCurrentShield()
    {
        return hasShield ? currentShield : 0;
    }

    // Implementaci�n de IHealth.GetMaxShield
    public int GetMaxShield()
    {
        return hasShield ? maxShield : 0;
    }

    // Implementaci�n de IHealth.IsFullHealth
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    // Implementaci�n de IHealth.IsFullShield
    public bool IsFullShield()
    {
        return hasShield ? currentShield >= maxShield : true;
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    void UpdateShieldBar()
    {
        // Seguridad: Si no hay barra asignada o no tiene escudo, no hacemos nada
        if (shieldBar == null || !hasShield) return;

        // 1. Calcular el valor exacto (usando float para que funcione con 50 de escudo)
        float porcentaje = (float)currentShield / maxShield;
        shieldBar.value = porcentaje;

        // 2. L�GICA DE APARICI�N / DESAPARICI�N
        if (currentShield <= 0)
        {
            // �ESCUDO ROTO! -> Ocultar la barra azul inmediatamente
            shieldBar.gameObject.SetActive(false);
        }
        else
        {
            // Si el escudo se ha regenerado (> 0) y las barras deber�an verse (combate)
            // entonces volvemos a mostrar la barra azul.
            if (healthBarVisible)
            {
                shieldBar.gameObject.SetActive(true);
            }
        }
    }

    // Implementaci�n de IHealth.Die
    public void Die()
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        isDead = true;
        Debug.Log($"[GeneralHealth] {name}: �General muerto!");

        // Sonido de muerte espec�fico para generales
        if (SoundColector.Instance != null)
        {
            SoundColector.Instance.PlayInfantryDeathAt(transform.position);
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

        // Ocultar UI
        HideHealthBars();

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
        if (isRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUnit(this);
        }
    }

    // Implementaci�n de IHealth.SetHealthBarVisible
    public void SetHealthBarVisible(bool visible)
    {
        if (visible)
        {
            ShowHealthBars();
        }
        else
        {
            HideHealthBars();
        }
    }

    // M�todo para revivir
    public void Revive()
    {
        if (isDead)
        {
            isDead = false;
            currentHealth = maxHealth;

            if (hasShield)
            {
                currentShield = maxShield;
                isShieldBroken = false;
            }

            gameObject.SetActive(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterUnit(this);
                isRegistered = true;
            }

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
            UpdateShieldBar();
        }
    }

    // M�todo para configurar como general
    public void SetAsGeneral(int shieldAmount, float regenRate = 1f)
    {
        hasShield = true;
        maxShield = shieldAmount;
        currentShield = shieldAmount;
        shieldRegenRate = regenRate;
        isShieldBroken = false;

        // Activar indicador visual
        if (generalIndicator != null)
            generalIndicator.SetActive(true);

        Debug.Log($"[GeneralHealth] {name}: Configurado como general con {shieldAmount} de escudo");
    }
}