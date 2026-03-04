using UnityEngine;
using System.Collections;

/// <summary>
/// Torre de vigil�ncia com comportamento AI simples:
/// - Detecta inimigos no alcance e escolhe o mais pr�ximo.
/// - Checa linha de vis�o opcionalmente.
/// - Rotaciona pivot/turret (opcional) para mirar no alvo.
/// - Dispara instanciando `bulletPrefab` (usa componente Bullet se presente).
/// Uso:
/// - Colocar no prefab da torre.
/// - Configurar `enemyLayerMask` (ou deixar vazio e usar `enemyTag`).
/// - Ajustar `firePoint`, `bulletPrefab`, `fireRate`, `range`, etc.
/// </summary>
public class SniperTower : MonoBehaviour
{
    [Header("Detec��o / Alvo")]
    public float range = 8f;
    public LayerMask enemyLayerMask;            // deixe 0 para fallback por tag
    public string enemyTag = "Enemy";           // usado no fallback
    public bool requireLineOfSight = true;
    public LayerMask obstacleMask;

    [Header("Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint;                 // ponto de onde as balas saem
    public float fireRate = 1f;                 // tiros por segundo
    public int bulletDamage = 10;
    public float bulletSpeed = 12f;
    public float aimDurationToSwitchTarget = 0.25f; // small delay to avoid thrashing targets

    [Header("Turret (opcional)")]
    public Transform turretPivot;               // transform que rotaciona para mirar (opcional)
    public float turretRotateSpeed = 10f;       // suavidade da rota��o

    // debug
    [Header("Debug")]
    public bool debugLogs = false;

    Transform currentTarget;
    float cooldown = 0f;
    float aimTimer = 0f;

    void Start()
    {
        // criar firePoint se n�o existir
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform, false);
            fp.transform.localPosition = Vector3.zero;
            firePoint = fp.transform;
        }
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        aimTimer -= Time.deltaTime;

        // Sempre procurar alvo (pode ser otimizado com coroutines ou eventos)
        FindTarget();

        // Rotacionar turret suavemente para o alvo (se houver pivot)
        if (turretPivot != null && currentTarget != null)
        {
            Vector3 dir = currentTarget.position - turretPivot.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.AngleAxis(angle, Vector3.forward);
            turretPivot.rotation = Quaternion.Lerp(turretPivot.rotation, targetRot, Time.deltaTime * turretRotateSpeed);
        }

        // Atirar se poss�vel
        if (currentTarget != null && cooldown <= 0f)
        {
            ShootAt(currentTarget);
            cooldown = 1f / Mathf.Max(0.0001f, fireRate);
        }
    }

    void FindTarget()
    {
        Transform best = null;
        float bestDist = Mathf.Infinity;

        // Primeiro tenta por LayerMask se foi definido
        if (enemyLayerMask.value != 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                if (!string.IsNullOrEmpty(enemyTag) && !c.CompareTag(enemyTag)) continue;

                // checar LOS se necess�rio
                if (requireLineOfSight)
                {
                    Vector2 dir = (c.transform.position - firePoint.position);
                    RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dir.normalized, dir.magnitude, obstacleMask);
                    if (hit.collider != null) continue; // bloqueado
                }

                float d = Vector2.SqrMagnitude((Vector2)(c.transform.position - transform.position));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c.transform;
                }
            }
        }
        else
        {
            // Fallback: procurar por tag no cen�rio (�til se LayerMask n�o foi configurado)
            var objs = GameObject.FindGameObjectsWithTag(enemyTag);
            for (int i = 0; i < objs.Length; i++)
            {
                var o = objs[i];
                if (o == null) continue;
                float dist = Vector2.Distance(transform.position, o.transform.position);
                if (dist > range) continue;

                if (requireLineOfSight)
                {
                    Vector2 dir = (o.transform.position - firePoint.position);
                    RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dir.normalized, dir.magnitude, obstacleMask);
                    if (hit.collider != null) continue;
                }

                float d = Vector2.SqrMagnitude((Vector2)(o.transform.position - transform.position));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = o.transform;
                }
            }
        }

        // Evita mudar de alvo constantemente se j� temos um alvo v�lido e a diferen�a � pequena
        if (best != null && currentTarget != null && best != currentTarget)
        {
            if (aimTimer > 0f)
            {
                // manter o target atual at� o timer expirar
                return;
            }
            else
            {
                aimTimer = aimDurationToSwitchTarget;
            }
        }

        currentTarget = best;
    }

    void ShootAt(Transform target)
    {
        if (bulletPrefab == null || firePoint == null || target == null) return;

        Vector3 dir = (target.position - firePoint.position).normalized;
        GameObject bgo = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        if (SoundColector.Instance != null)SoundColector.Instance.PlayTowerShotAt(firePoint.position);

        Bullet b = bgo.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(dir);
            b.isEnemyBullet = false;
            b.damage = bulletDamage;
            b.speed = bulletSpeed;
        }
        else
        {
            var rb = bgo.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * bulletSpeed;
        }

        // girar proj�til para dire��o
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bgo.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // opcional: destruir proj�til ap�s tempo (evita acumular)
        Destroy(bgo, 5f);

        if (debugLogs)
            Debug.Log($"[SniperTower] Atirou em {target.name}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, range);

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}