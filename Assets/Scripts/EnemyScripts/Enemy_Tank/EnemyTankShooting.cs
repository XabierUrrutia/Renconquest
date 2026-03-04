using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyTankController))]
[RequireComponent(typeof(TankVisuals))]
public class EnemyTankShooting : MonoBehaviour
{
    [Header("Configuraci�n de Arma")]
    public GameObject bulletPrefab;
    public Transform weaponPoint;
    public float fireRate = 2.5f;
    public float bulletSpeed = 10f;
    public int bulletDamage = 15;

    [Header("Inteligencia")]
    public float visionRange = 15f;
    public float attackRange = 7f;
    public float intervaloBusqueda = 1.0f;

    [Header("Referencias")]
    public Transform playerBase;

    // --- VARIABLES INTERNAS (CORREGIDAS) ---
    // Aqu� estaba el error: ahora est� escrito con 'o' para coincidir con el resto
    private List<Transform> jogadoresDisponiveis = new List<Transform>();

    private float tiempoUltimaBusqueda = 0f;
    private Transform currentTarget;
    // ---------------------------------------------

    private EnemyTankController controller;
    private TankVisuals visual;
    private float nextFireTime;
    private Collider2D myCollider;

    void Start()
    {
        controller = GetComponent<EnemyTankController>();
        visual = GetComponent<TankVisuals>();
        myCollider = GetComponent<Collider2D>();

        // 1. Buscar la Base al inicio
        BuscarBase();

        // 2. Hacer una primera b�squeda de jugadores
        BuscarTodosJogadores();
    }

    void Update()
    {
        // --- L�GICA DE RADAR ---
        if (Time.time - tiempoUltimaBusqueda >= intervaloBusqueda)
        {
            BuscarTodosJogadores();
            tiempoUltimaBusqueda = Time.time;

            if (playerBase == null) BuscarBase();
        }

        DecidirObjetivo();
        ComportamientoDeCombate();
    }

    void BuscarTodosJogadores()
    {
        // Ahora s� funcionar� porque la variable de arriba se llama igual
        jogadoresDisponiveis.Clear();

        GameObject[] todosJogadores = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject jogadorObj in todosJogadores)
        {
            IHealth health = jogadorObj.GetComponent<IHealth>();
            if (health != null && !health.IsDead && jogadorObj.activeInHierarchy)
            {
                jogadoresDisponiveis.Add(jogadorObj.transform);
            }
        }
    }

    Transform EncontrarJogadorMaisProximo()
    {
        if (jogadoresDisponiveis.Count == 0) return null;

        Transform mejorCandidato = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Transform t in jogadoresDisponiveis)
        {
            if (t == null) continue;

            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                mejorCandidato = t;
            }
        }
        return mejorCandidato;
    }

    void DecidirObjetivo()
    {
        currentTarget = null;

        Transform playerCercano = EncontrarJogadorMaisProximo();
        float distPlayer = 9999f;

        if (playerCercano != null)
        {
            distPlayer = Vector2.Distance(transform.position, playerCercano.position);
        }

        // Si hay jugador Y est� dentro del Rango de Visi�n -> ATACAR JUGADOR
        if (playerCercano != null && distPlayer <= visionRange)
        {
            currentTarget = playerCercano;
        }
        // Si no, -> ATACAR BASE
        else if (playerBase != null)
        {
            currentTarget = playerBase;
        }
    }

    void ComportamientoDeCombate()
    {
        if (currentTarget == null) return;

        float distancia = Vector2.Distance(transform.position, currentTarget.position);

        if (distancia <= attackRange)
        {
            controller.StopMoving();

            Vector2 direccion = (currentTarget.position - transform.position).normalized;
            if (visual != null) visual.FaceDirection(direccion);

            if (Time.time >= nextFireTime)
            {
                Disparar(currentTarget);
            }
        }
        else
        {
            controller.SetTarget(currentTarget.position);
        }
    }

    void BuscarBase()
    {
        GameObject baseObj = GameObject.FindGameObjectWithTag("PlayerBase");
        if (baseObj != null) playerBase = baseObj.transform;
    }

    void Disparar(Transform target)
    {
        if (bulletPrefab == null || weaponPoint == null) return;

        nextFireTime = Time.time + fireRate;
        Vector2 direction = (target.position - transform.position).normalized;

        if (visual != null)
        {
            visual.FaceDirection(direction);
            visual.TriggerShootAnim();
        }

        GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);

        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
        if (myCollider != null && bulletCollider != null) Physics2D.IgnoreCollision(myCollider, bulletCollider);

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(direction);
            b.isEnemyBullet = true;
            b.damage = bulletDamage;
            b.speed = bulletSpeed;
        }
        else
        {
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * bulletSpeed;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        Destroy(bullet, 5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }

    void OnDestroy()
    {
        if (EnemyManager.Instance != null) EnemyManager.Instance.RemoverTanque(this);
    }
}