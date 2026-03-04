using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Componente para adicionar vida (HP) a uma Base do jogador.
/// - Regista a base no EnemyManager para que inimigos possam considerá-la como alvo.
/// - Aceita dano por balas inimigas (Bullet.OnTriggerEnter2D já aplica dano a objetos com tag "Player").
/// - Ao morrer carrega imediatamente a cena "Game Over".
/// - IMPLEMENTA IHealth para compatibilidade com el sistema de salud unificado.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBase : MonoBehaviour, IHealth
{
    [Header("HP")]
    [Tooltip("Vida máxima da base")]
    public int maxHealth = 1000;
    [Tooltip("Slider opcional para mostrar a vida")]
    public Slider healthBar;

    [Header("Tag")]
    [Tooltip("Tag a aplicar ao GameObject para que balas inimigas o reconheçam (deixe vazio para não alterar)")]
    public string runtimeTagToApply = "Player";

    [Header("Feedback")]
    [Tooltip("Tempo durante o qual o feedback de dano permanece (se aplicável)")]
    public float damageFeedbackSeconds = 0.2f;

    [Header("Cores do Slider")]
    [Tooltip("Cor cuando em bom estado (acima de 50 HP)")]
    public Color healthyColor = Color.green;
    [Tooltip("Cor cuando em aviso (<= 50 HP)")]
    public Color warningColor = Color.yellow;
    [Tooltip("Cor cuando crítico (<= 30% da vida máxima)")]
    public Color criticalColor = Color.red;

    private int currentHealth;
    private bool isDestroyed = false;

    // IMPLEMENTACIÓN DE IHealth
    public bool IsDead => isDestroyed;
    public Transform Transform => transform;

    void Awake()
    {
        currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);
    }

    void Start()
    {
        // Aplica tag se definida e existir
        if (!string.IsNullOrEmpty(runtimeTagToApply))
        {
            try
            {
                gameObject.tag = runtimeTagToApply;
            }
            catch
            {
                Debug.LogWarning($"[PlayerBase] Tag '{runtimeTagToApply}' não existe. Defina a tag manualmente no Inspector se necessário.");
            }
        }

        // Inicializa UI
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            healthBar.gameObject.SetActive(true);
            UpdateHealthBarColor();
        }

        // Registar como "jogador" para que EnemyManager e inimigos o conheçam
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegistrarNovoJogador(this.transform);
            Debug.Log($"[PlayerBase] Registrada no EnemyManager: {name}");
        }

        // Registrar en GameManager como unidad IHealth
        if (GameManager.Instance != null)
        {
            // Como PlayerBase implementa IHealth, se registrará automáticamente
            // al llamar al método RegisterUnit
            IHealth healthInterface = this as IHealth;
            GameManager.Instance.RegisterUnit(healthInterface);
        }
    }

    /// <summary>
    /// Força a reinicialização do HP (útil se maxHealth for alterado em runtime)
    /// </summary>
    public void ForceInitializeHealth()
    {
        if (currentHealth <= 0)
        {
            currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);
            Debug.Log($"[PlayerBase] HP forçado para {currentHealth}/{maxHealth}");
        }
    }
    /// <summary>
    /// Aplica dano à base. Chamado por balas inimigas ou outras fontes.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;
        if (amount <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        
        GameEvents.RaiseBaseUnderAttack();


        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            UpdateHealthBarColor();
        }

        // opcional: efeito visual / som (pode ligar aqui)
        StartCoroutine(DamageFeedbackCoroutine());

        Debug.Log($"[PlayerBase] {name} recebeu {amount} de dano. HP = {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }

    // IMPLEMENTACIÓN DE IHealth.Die
    public void Die()
    {
        OnDestroyed();
    }

    System.Collections.IEnumerator DamageFeedbackCoroutine()
    {
        // Placeholder para feedback (piscar sprite, etc.)
        if (damageFeedbackSeconds > 0f)
            yield return new WaitForSeconds(damageFeedbackSeconds);
        else
            yield break;
    }

    void OnDestroyed()
    {
        SoundColector.Instance?.PlayBuildingDestroyedAt(transform.position);

        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"[PlayerBase] {name} FOI DESTRUID. Carregando Game Over...");

        // Notificar al GameManager que la base ha muerto
        if (GameManager.Instance != null)
        {
            IHealth healthInterface = this as IHealth;
            GameManager.Instance.UnregisterUnit(healthInterface);
            GameManager.Instance.ResetGame();
        }

        // Ao destruir, avisar EnemyManager para remover como alvo (fallback)
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RemoverJogador(this.transform);
        }

        // Carregar cena de Game Over (nome deve corresponder ao build settings)
        SoundColector.Instance?.PlayBuildingDestroyedAt(transform.position);
        SoundColector.Instance?.PlayDefeatMusic();
        SceneManager.LoadScene("Game Over");
    }

    void OnDestroy()
    {
        // Ao destruir, avisar EnemyManager para remover como alvo (fallback)
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RemoverJogador(this.transform);
        }

        // Desregistrar del GameManager
        if (GameManager.Instance != null)
        {
            IHealth healthInterface = this as IHealth;
            GameManager.Instance.UnregisterUnit(healthInterface);
        }
    }

    // IMPLEMENTACIÓN DE IHealth.GetCurrentHealth
    public int GetCurrentHealth() => currentHealth;

    // IMPLEMENTACIÓN DE IHealth.GetMaxHealth
    public int GetMaxHealth() => maxHealth;

    // IMPLEMENTACIÓN DE IHealth.IsFullHealth
    public bool IsFullHealth() => currentHealth >= maxHealth;

    // IMPLEMENTACIÓN DE IHealth.Heal
    /// <summary>
    /// Permite curar a base via código, se necessário
    /// </summary>
    public void Heal(int amount)
    {
        if (isDestroyed || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            UpdateHealthBarColor();
        }
    }

    // IMPLEMENTACIÓN DE IHealth.SetHealthBarVisible
    public void SetHealthBarVisible(bool visible)
    {
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(visible);
        }
    }

    // IMPLEMENTACIÓN DE IHealth - Métodos de escudo (la base no tiene escudo)
    public int GetCurrentShield() => 0;
    public int GetMaxShield() => 0;
    public bool IsFullShield() => true;
    public void RepairShield(int amount)
    {
        Debug.Log($"[PlayerBase] Intentando reparar escudo en base (no tiene escudo)");
    }

    // Atualiza a cor do fill do slider conforme thresholds:
    // - crítico: <= 30% da vida máxima -> vermelho
    // - aviso: <= 50 HP -> amarelo
    // - saudável: caso contrário -> verde (ou healthyColor)
    void UpdateHealthBarColor()
    {
        if (healthBar == null) return;

        Image fillImage = null;
        if (healthBar.fillRect != null)
            fillImage = healthBar.fillRect.GetComponent<Image>();

        if (fillImage == null)
        {
            // tenta encontrar Image em filho chamado "Fill"
            var img = healthBar.GetComponentInChildren<Image>();
            if (img != null) fillImage = img;
        }

        if (fillImage == null) return;

        // prioridade ao crítico (30% da vida máxima)
        float criticalThreshold = maxHealth * 0.3f;
        if (currentHealth <= criticalThreshold)
        {
            fillImage.color = criticalColor;
        }
        else if (currentHealth <= 250)
        {
            fillImage.color = warningColor;
        }
        else
        {
            fillImage.color = healthyColor;
        }
    }
}