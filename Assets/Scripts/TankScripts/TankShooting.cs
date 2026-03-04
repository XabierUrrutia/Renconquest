using UnityEngine;
using System.Collections;
using TMPro; // Si usas TextMeshPro para la munición

public class TankShooting : MonoBehaviour
{
    [Header("Referencias Clave")]
    public TankVisuals tankVisuals; // Arrastra aquí tu script TankVisuals
    public Transform firePoint;     // Desde donde sale la bala (centro o cañón)

    [Header("Ajustes de Arma")]
    public GameObject bulletPrefab;
    public float fireRate = 2.0f;   // Los tanques disparan más lento
    public int maxAmmo = 50;        // Más munición que el soldado
    public string weaponName = "CANNON 120mm";

    [Header("Bala")]
    public float bulletSpeed = 12f;
    public int bulletDamage = 100; // Daño alto (mata soldados de 1 golpe)

    [Header("Auto-Aim (Cerebro)")]
    public float detectionRange = 10f; // Rango del tanque
    public LayerMask enemyLayerMask;
    public bool autoAimEnabled = true;
    public float aimUpdateRate = 0.2f;

    [Header("Comportamiento")]
    public float accuracy = 0.98f; // Los tanques suelen ser precisos

    [Header("Visualización Rango (Debug)")]
    public bool showRangeInGame = true;
    public Color rangeColor = new Color(1f, 0f, 0f, 0.2f); // Rojo para tanque

    // Estado interno
    public int currentAmmo;
    private float nextFireTime;
    private Transform currentTarget;
    private Coroutine aimCoroutine;
    private LineRenderer rangeCircle;

    // Referencia opcional si usas veterancía
    private UnitVeterancy myVeterancy;

    void Start()
    {
        currentAmmo = maxAmmo;
        myVeterancy = GetComponent<UnitVeterancy>();

        // Si no asignaste firePoint, usa la propia posición
        if (firePoint == null) firePoint = transform;

        // Visualización del círculo de rango
        if (showRangeInGame)
        {
            CreateRangeVisualization();
        }

        // Iniciar la IA de búsqueda de objetivos
        if (autoAimEnabled)
        {
            aimCoroutine = StartCoroutine(UpdateAimTarget());
        }
    }

    void Update()
    {
        // 1. Lógica de Disparo Automático (Si hay objetivo y está en rango)
        if (currentTarget != null && autoAimEnabled)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

            // Doble chequeo de rango por seguridad
            if (distanceToTarget <= detectionRange)
            {
                if (Time.time >= nextFireTime && currentAmmo > 0)
                {
                    Shoot(currentTarget.position);
                    nextFireTime = Time.time + fireRate;
                }
            }
            else
            {
                currentTarget = null; // El objetivo se escapó
            }
        }

        // 2. Recarga (Simplificada para tanque: si se queda a 0, recarga solo tras un tiempo)
        if (currentAmmo <= 0)
        {
            // Aquí podrías poner una lógica de recarga si quieres, 
            // o simplemente dejar que se quede sin balas.
            // Por ahora, recarga mágica tras 3 segundos para no bloquear el juego:
            Invoke("ReloadMagically", 3f);
        }
    }

    void ReloadMagically()
    {
        if (currentAmmo <= 0) currentAmmo = maxAmmo;
    }

    void Shoot(Vector3 targetPos)
    {
        // 1. Calcular dirección hacia el enemigo
        Vector2 directionToTarget = (targetPos - transform.position).normalized;

        // 2. OBLIGAR al visual a mirar hacia ahí antes de animar
        if (tankVisuals != null)
        {
            tankVisuals.FaceDirection(directionToTarget); // <--- ESTO GIRA EL TANQUE
            tankVisuals.TriggerShootAnim(); // <--- ESTO INICIA EL RETROCESO
        }

        // 3. Aplicar Imprecisión a la bala
        Vector2 bulletDir = directionToTarget;
        if (accuracy < 1.0f)
        {
            float inaccuracy = (1.0f - accuracy) * 2.0f;
            bulletDir.x += Random.Range(-inaccuracy, inaccuracy);
            bulletDir.y += Random.Range(-inaccuracy, inaccuracy);
            bulletDir.Normalize();
        }

        // 4. Instanciar Bala
        // (Nota: Usamos firePoint.position, que ahora estará "conceptualmente" orientado,
        // aunque si tu firePoint es un objeto hijo fijo, saldrá del mismo sitio. 
        // En isométrico 2D simple suele bastar con que salga del centro o del pivote).
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();

        if (b != null)
        {
            b.SetDirection(bulletDir);
            b.isEnemyBullet = false;
            b.damage = bulletDamage;
            b.speed = bulletSpeed;
            if (myVeterancy != null) b.ownerVeterancy = myVeterancy;
        }

        if (SoundColector.Instance != null) SoundColector.Instance.PlayTankShotAt(firePoint.position);

        currentAmmo--;
    }

    // --------------------------------------------------------------------------
    // LÓGICA DE BÚSQUEDA DE OBJETIVOS (Copiada y adaptada de PlayerShooting)
    // --------------------------------------------------------------------------

    IEnumerator UpdateAimTarget()
    {
        while (true)
        {
            FindNearestEnemy();
            yield return new WaitForSeconds(aimUpdateRate);
        }
    }

    void FindNearestEnemy()
    {
        // 1. Detectar todo alrededor (sin filtrar capas por ahora para probar)
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(transform.position, detectionRange);

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;
        int enemiesFoundCount = 0;

        foreach (Collider2D col in allColliders)
        {
            // Solo nos interesan los que tengan el Tag "Enemy"
            if (col.CompareTag("Enemy"))
            {
                enemiesFoundCount++;
                float distance = Vector2.Distance(transform.position, col.transform.position);

                // --- SIN RAYCAST (Visión de Rayos X activada para probar) ---
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = col.transform;
                }
            }
        }

        currentTarget = nearestEnemy;

        // --- MENSAJES DE DIAGNÓSTICO (Miralos en la Consola) ---
        if (currentTarget != null)
        {
            Debug.Log($"<color=green>OBJETIVO FIJADO: {currentTarget.name}</color>");
        }
        else if (enemiesFoundCount > 0)
        {
            Debug.Log($"<color=orange>Veo {enemiesFoundCount} enemigos, pero no he seleccionado ninguno (Raro).</color>");
        }
        else
        {
            // Si sale esto, el problema es que Unity no detecta los Colliders
            // Debug.Log("Escaneando... No veo nada con el tag 'Enemy'."); 
        }
    }

    // --------------------------------------------------------------------------
    // VISUALIZACIÓN (Gizmos y Círculo)
    // --------------------------------------------------------------------------

    void CreateRangeVisualization()
    {
        GameObject rangeObject = new GameObject("TankRangeVis");
        rangeObject.transform.SetParent(transform);
        rangeObject.transform.localPosition = Vector3.zero;

        rangeCircle = rangeObject.AddComponent<LineRenderer>();
        rangeCircle.material = new Material(Shader.Find("Sprites/Default"));
        rangeCircle.startColor = rangeColor;
        rangeCircle.endColor = rangeColor;
        rangeCircle.startWidth = 0.1f; // Un poco más grueso que el del soldado
        rangeCircle.endWidth = 0.1f;
        rangeCircle.useWorldSpace = false;
        rangeCircle.loop = true;

        DrawCircle(rangeCircle, detectionRange, 50);
    }

    void DrawCircle(LineRenderer lineRenderer, float radius, int segments)
    {
        lineRenderer.positionCount = segments + 1;
        float angle = 0f;
        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += 360f / segments;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar rango en el editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Dibujar línea al objetivo actual
        if (currentTarget != null && firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, currentTarget.position);
        }
    }
}