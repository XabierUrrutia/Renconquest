using System.Collections;
using UnityEngine;

/// <summary>
/// Ataque explosivo del boss. Se añade automáticamente al boss al spawnear.
/// Tiene dos modos: ataque normal (ya lo tiene EnemyShooting) y ataque cargado en área.
/// </summary>
public class BossExplosiveAttack : MonoBehaviour
{
    [Header("Ataque Explosivo")]
    [Tooltip("Radio del área de explosión")]
    public float explosionRadius = 3f;
    [Tooltip("Daño de la explosión")]
    public int explosionDamage = 5;
    [Tooltip("Tiempo entre ataques explosivos")]
    public float explosionCooldown = 8f;
    [Tooltip("Tiempo de carga antes de explotar")]
    public float chargeTime = 2f;

    [Header("Visual")]
    [Tooltip("Color del boss durante la carga")]
    public Color chargeColor = Color.red;
    [Tooltip("Prefab de efecto de explosión (opcional)")]
    public GameObject explosionEffectPrefab;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float timer = 0f;
    private bool isCharging = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Empezar con un delay aleatorio para que no explote inmediatamente
        timer = Random.Range(3f, explosionCooldown);
    }

    void Update()
    {
        if (Time.timeScale == 0) return;
        if (isCharging) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            StartCoroutine(ChargeAndExplode());
            timer = explosionCooldown;
        }
    }

    IEnumerator ChargeAndExplode()
    {
        isCharging = true;

        // Fase de carga — parpadeo rojo
        float elapsed = 0f;
        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(originalColor, chargeColor, t);
            yield return null;
        }

        // Restaurar color
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        // Explotar
        Explode();
        isCharging = false;
    }

    void Explode()
    {
        // Efecto visual
        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // Daño en área a todos los jugadores cercanos
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IHealth health = hit.GetComponent<IHealth>();
                if (health != null && !health.IsDead)
                {
                    health.TakeDamage(explosionDamage);
                    Debug.Log($"[BossExplosiveAttack] Explosión dañó a {hit.name} por {explosionDamage}");
                }
            }
        }

        Debug.Log($"[BossExplosiveAttack] ¡EXPLOSIÓN! Radio: {explosionRadius}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
