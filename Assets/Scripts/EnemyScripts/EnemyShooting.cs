using UnityEngine;
using System.Collections;

public class EnemyShooting : MonoBehaviour
{
    [Header("Configuraci�n de Disparo")]
    public GameObject bulletPrefab;
    public Transform weaponPoint;
    public float fireRate = 1.5f;
    public float bulletSpeed = 8f;
    public float attackRange = 6f;
    public int bulletDamage = 1;

    [Header("Configuraci�n Visual")]
    public SpriteRenderer spriteRenderer;
    public float shootingSpriteDuration = 0.2f;

    [Header("Sprites - APUNTADO (Aiming)")]
    public Sprite aimingSpriteRight; // Arrastra imagen apuntando Derecha
    public Sprite aimingSpriteLeft;  // Arrastra imagen apuntando Izquierda

    [Header("Sprites - DISPARO (Shooting)")]
    public Sprite shootingSpriteRight; // Arrastra imagen disparando Derecha
    public Sprite shootingSpriteLeft;  // Arrastra imagen disparando Izquierda

    [Header("Referencias")]
    public Transform player;

    private float nextFireTime;
    private EnemyAI enemyAI;
    private EnemyController enemyController;
    private Transform baseJogador;

    // Estado interno
    private bool isShootingAction = false; // True solo durante el instante del disparo

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        enemyController = GetComponent<EnemyController>();

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyAI != null) baseJogador = enemyAI.baseJogador;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        Transform target = GetCurrentTarget();

        // L�gica Principal de Sprites y Disparo
        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.position);

            // �Est� dentro del rango de ataque?
            if (dist <= attackRange)
            {
                // 1. GESTIONAR APUNTADO Y DISPARO
                ManageCombatState(target);
            }
            else
            {
                // 2. EST� LEJOS: Devolver control al EnemyController (Caminar)
                UnlockAnimation();
            }
        }
        else
        {
            // NO HAY OBJETIVO: Devolver control al EnemyController
            UnlockAnimation();
        }
    }

    void ManageCombatState(Transform target)
    {
        // Si estamos en medio de la animaci�n de disparo (el retroceso), no hacemos nada visual aqu�
        if (isShootingAction) return;

        // --- ESTADO: APUNTANDO (AIMING) ---

        // 1. Bloqueamos al controlador de movimiento para que no ponga sprites de caminar
        if (enemyController != null) enemyController.bloquearAnimacion = true;

        // 2. Calculamos direcci�n
        bool isRight = (target.position.x > transform.position.x);

        // 3. Ponemos el sprite de APUNTADO correspondiente
        if (spriteRenderer != null)
        {
            if (isRight)
                spriteRenderer.sprite = (aimingSpriteRight != null) ? aimingSpriteRight : spriteRenderer.sprite;
            else
                spriteRenderer.sprite = (aimingSpriteLeft != null) ? aimingSpriteLeft : (aimingSpriteRight); // Fallback
        }

        // --- L�GICA DE DISPARO ---
        // Verificamos si toca disparar
        bool condicionesDisparo = Time.time >= nextFireTime &&
                                  (enemyAI == null || enemyAI.EstaPersiguiendoJogador() || target == baseJogador);

        if (condicionesDisparo)
        {
            StartCoroutine(ShootSequence(target, isRight));
            nextFireTime = Time.time + fireRate;
        }
    }

    IEnumerator ShootSequence(Transform target, bool isRight)
    {
        isShootingAction = true;

        // --- ESTADO: DISPARANDO (SHOOTING) ---

        // 1. Asegurar bloqueo (por si acaso)
        if (enemyController != null) enemyController.bloquearAnimacion = true;

        // 2. Poner sprite de DISPARO
        if (spriteRenderer != null)
        {
            if (isRight)
                spriteRenderer.sprite = (shootingSpriteRight != null) ? shootingSpriteRight : aimingSpriteRight;
            else
                spriteRenderer.sprite = (shootingSpriteLeft != null) ? shootingSpriteLeft : shootingSpriteRight;
        }

        // 3. Crear bala
        FireBullet(target);

        // 4. Esperar lo que dura el sprite de "fogonazo"
        yield return new WaitForSeconds(shootingSpriteDuration);

        // 5. Fin de la acci�n de disparo. 
        // NO desbloqueamos aqu� manualmente. El Update lo har� autom�ticamente:
        // - Si sigue habiendo enemigo cerca -> El Update pondr� sprite de APUNTAR.
        // - Si el enemigo muri� o se alej� -> El Update llamar� a UnlockAnimation().

        isShootingAction = false;
    }

    void UnlockAnimation()
    {
        // Si estamos disparando, no desbloqueamos nada todav�a
        if (isShootingAction) return;

        // Devolvemos el control al script de caminar
        if (enemyController != null)
        {
            enemyController.bloquearAnimacion = false;
        }
    }

    Transform GetCurrentTarget()
    {
        if (baseJogador == null && enemyAI != null) baseJogador = enemyAI.baseJogador;

        if (baseJogador != null)
        {
            float distBase = Vector2.Distance(transform.position, baseJogador.position);
            if (distBase <= attackRange * 1.2f) return baseJogador;
        }

        if (enemyAI != null)
        {
            Transform aiTarget = enemyAI.GetJogadorAlvo();
            if (aiTarget != null) return aiTarget;
        }

        return player;
    }

    void FireBullet(Transform target)
    {
        if (bulletPrefab == null || weaponPoint == null || target == null) return;

        Vector2 dir = (target.position - weaponPoint.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);

        if (SoundColector.Instance != null)
        {
            bool isTank = GetComponentInParent<TankVisuals>() != null;
            if (isTank) SoundColector.Instance.PlayTankShotAt(weaponPoint.position);
            else SoundColector.Instance.PlayInfantryShotAt(weaponPoint.position);
        }

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(dir);
            b.isEnemyBullet = true;
            b.damage = bulletDamage;
            b.speed = bulletSpeed;
        }
        else
        {
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * bulletSpeed;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Destroy(bullet, 3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}