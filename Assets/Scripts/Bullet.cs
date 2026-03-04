using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public Rigidbody2D rb;

    // Flag para identificar origen da bala
    public bool isEnemyBullet = false;

    // --- VETERAN�A ---
    [HideInInspector] public UnitVeterancy ownerVeterancy;
    public int xpPorGolpe = 10;
    // -----------------

    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, 3f);
    }

    void OnTriggerEnter2D(Collider2D hit)
    {
        // =====================================================================
        // 1. BALA DEL JUGADOR (Da�a a enemigos)
        // =====================================================================
        if (!isEnemyBullet)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Busca cualquier script de vida enemiga conocido
                var tutorialEnemy = hit.GetComponent<TutorialEnemyHealth>();
                var classicEnemy = hit.GetComponent<EnemyHealth>();
                var generalEnemy = hit.GetComponent<EnemyGeneralHealth>();

                // Buscar en el padre si no est� en el objeto directo
                if (tutorialEnemy == null && hit.transform.parent != null)
                    tutorialEnemy = hit.transform.parent.GetComponent<TutorialEnemyHealth>();

                if (classicEnemy == null && hit.transform.parent != null)
                    classicEnemy = hit.transform.parent.GetComponent<EnemyHealth>();

                if (generalEnemy == null && hit.transform.parent != null)
                    generalEnemy = hit.transform.parent.GetComponent<EnemyGeneralHealth>();

                bool huboImpacto = false;

                if (tutorialEnemy != null) { tutorialEnemy.TakeDamage(damage); huboImpacto = true; }
                else if (classicEnemy != null) { classicEnemy.TakeDamage(damage); huboImpacto = true; }
                else if (generalEnemy != null) { generalEnemy.TakeDamage(damage); huboImpacto = true; }

                if (huboImpacto)
                {
                    if (ownerVeterancy != null) ownerVeterancy.GanarXP(xpPorGolpe);
                    Destroy(gameObject);
                }
                return;
            }
        }

        // =====================================================================
        // 2. BALA ENEMIGA (Da�a a Jugadores, Generales, Bases y ALIADOS)
        // =====================================================================
        else // (isEnemyBullet == true)
        {
            // CAMBIO CLAVE: Ya no miramos Tags espec�ficos (Player, General...).
            // Simplemente decimos: Si NO es un enemigo y NO es un obst�culo, intenta herirlo.
            if (!hit.CompareTag("Enemy") && !hit.CompareTag("Obstacle") && !hit.CompareTag("Wall") && !hit.CompareTag("Terrain"))
            {
                // 1. Intentar buscar Interfaz gen�rica
                IHealth health = hit.GetComponent<IHealth>();
                if (health == null && hit.transform.parent != null) health = hit.transform.parent.GetComponent<IHealth>();

                // 2. Si no hay interfaz, buscar scripts espec�ficos uno por uno
                if (health == null)
                {
                    // Jugador
                    var ph = hit.GetComponent<PlayerHealth>();
                    if (ph != null) health = ph as IHealth;

                    // General
                    if (health == null)
                    {
                        var gh = hit.GetComponent<GeneralHealth>();
                        if (gh != null) health = gh as IHealth;
                    }

                    // Base
                    if (health == null)
                    {
                        var pb = hit.GetComponent<PlayerBase>();
                        if (pb != null) health = pb as IHealth;
                    }

                    // --- NUEVO: TORRES Y SOLDADOS ALIADOS ---
                    // Si tus soldados usan "TowerHealth" o un script propio, a��delo aqu�
                    if (health == null)
                    {
                        var th = hit.GetComponent<TowerHealth>(); // <--- IMPORTANTE
                        if (th != null) health = th as IHealth;
                    }
                }

                // 3. Aplicar Da�o si encontramos algo con vida
                if (health != null)
                {
                    if (!health.IsDead)
                    {
                        health.TakeDamage(damage);
                    }
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    // Caso especial para PlayerBase antiguo sin interfaz
                    PlayerBase baseComp = hit.GetComponent<PlayerBase>();
                    if (baseComp != null)
                    {
                        baseComp.TakeDamage(damage);
                        Destroy(gameObject);
                        return;
                    }
                }
            }
        }

        // =====================================================================
        // 3. OBST�CULOS (Paredes, Terreno, etc)
        // =====================================================================
        if (hit.CompareTag("Obstacle") || hit.CompareTag("Wall") || hit.CompareTag("Terrain"))
        {
            Destroy(gameObject);
            return;
        }

        // Capas f�sicas (Layer check)
        if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Destroy(gameObject);
            return;
        }
    }
}