using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Referencias de Objetivos")]
    public Transform playerBase;
    public Transform player; // Jugador principal (para compatibilidad)

    [Header("Gestión de Múltiples Jugadores")]
    private List<Transform> todosJogadores = new List<Transform>();

    // LISTAS SEPARADAS PARA DISTINTOS TIPOS DE ENEMIGOS
    private List<EnemyAI> todosEnemigos = new List<EnemyAI>(); // Soldados
    private List<EnemyTankShooting> todosTanques = new List<EnemyTankShooting>(); // Tanques

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Forzar la búsqueda de la base incluso si está asignada (por si acaso)
        BuscarYAsignarBase();

        // Buscar y registrar todos los jugadores existentes
        BuscarYRegistrarTodosJogadores();
    }

    void Start()
    {
        // Registrar todos los enemigos existentes en la escena
        RegistrarEnemigosExistentes();
    }

    void BuscarYAsignarBase()
    {
        // Si ya está asignada, verificar que no esté en (0,0,0)
        if (playerBase != null && playerBase.position != Vector3.zero)
        {
            return;
        }

        GameObject baseObj = GameObject.FindGameObjectWithTag("PlayerBase");

        if (baseObj == null)
        {
            // Buscar por nombres alternativos
            string[] nombresAlternativos = { "Base", "MainBase", "Castle", "HomeBase", "PlayerBase" };
            foreach (string nombre in nombresAlternativos)
            {
                baseObj = GameObject.Find(nombre);
                if (baseObj != null) break;
            }
        }

        if (baseObj != null)
        {
            playerBase = baseObj.transform;
            Debug.Log("Base encontrada y asignada: " + playerBase.name + " en posición: " + playerBase.position);

            if (playerBase.position == Vector3.zero)
            {
                Debug.LogError("¡LA BASE ESTÁ EN (0,0,0)! Verifica la posición en la escena.");
            }
        }
        else
        {
            Debug.LogError("NO SE PUDO ENCONTRAR LA BASE DEL JUGADOR");
        }
    }

    void BuscarYRegistrarTodosJogadores()
    {
        todosJogadores.Clear();

        // Buscar todos los objetos con tag "Player"
        GameObject[] jogadoresEncontrados = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject jogadorObj in jogadoresEncontrados)
        {
            IHealth health = jogadorObj.GetComponent<IHealth>();
            if (health != null && !health.IsDead && jogadorObj.activeInHierarchy)
            {
                todosJogadores.Add(jogadorObj.transform);
            }
        }

        // Mantener compatibilidad con el jugador principal
        if (player == null && todosJogadores.Count > 0)
        {
            player = todosJogadores[0];
        }

        Debug.Log($"Jugadores encontrados (IHealth): {todosJogadores.Count}");
    }

    void RegistrarEnemigosExistentes()
    {
        todosEnemigos.Clear();
        todosTanques.Clear();

        // 1. Registrar Soldados (EnemyAI)
        EnemyAI[] enemigosEncontrados = FindObjectsOfType<EnemyAI>();
        todosEnemigos.AddRange(enemigosEncontrados);

        // Notificar a cada soldado sobre los jugadores (porque EnemyAI usa un sistema de lista interna)
        foreach (EnemyAI enemigo in todosEnemigos)
        {
            if (enemigo != null)
            {
                foreach (Transform jogador in todosJogadores)
                {
                    enemigo.AdicionarJogador(jogador);
                }
            }
        }

        // 2. Registrar Tanques (EnemyTankShooting)
        EnemyTankShooting[] tanquesEncontrados = FindObjectsOfType<EnemyTankShooting>();
        todosTanques.AddRange(tanquesEncontrados);

        // Nota: Los tanques usan un sistema de búsqueda en Update (GetJogadorMaisProximo), 
        // por lo que no necesitan "AdicionarJogador" explícitamente, pero quedan registrados para el conteo.
    }

    // MÉTODOS PÚBLICOS PARA GESTIÓN DE JUGADORES

    public void RegistrarNovoJogador(Transform novoJogador)
    {
        if (!todosJogadores.Contains(novoJogador))
        {
            IHealth health = novoJogador.GetComponent<IHealth>();
            if (health != null && !health.IsDead)
            {
                todosJogadores.Add(novoJogador);

                // Notificar a los soldados (EnemyAI)
                foreach (EnemyAI enemy in todosEnemigos)
                {
                    if (enemy != null) enemy.AdicionarJogador(novoJogador);
                }

                // Los tanques lo detectarán automáticamente en su próximo ciclo de búsqueda

                Debug.Log($"Nuevo jugador registrado: {novoJogador.name}. Total: {todosJogadores.Count}");
            }
        }
    }

    public void RemoverJogador(Transform jogadorMorto)
    {
        if (todosJogadores.Contains(jogadorMorto))
        {
            todosJogadores.Remove(jogadorMorto);

            // Notificar a los soldados
            foreach (EnemyAI enemy in todosEnemigos)
            {
                if (enemy != null) enemy.RemoverJogador(jogadorMorto);
            }

            // Actualizar referencia 'player' si es necesario
            if (player == jogadorMorto && todosJogadores.Count > 0)
            {
                player = todosJogadores[0];
            }
            else if (todosJogadores.Count == 0)
            {
                player = null;
            }

            Debug.Log($"Jugador removido: {jogadorMorto.name}. Total: {todosJogadores.Count}");
        }
    }

    // --- GESTIÓN DE ENEMIGOS (SOLDADOS) ---

    public void RegistrarEnemy(EnemyAI novoEnemy)
    {
        if (!todosEnemigos.Contains(novoEnemy))
        {
            todosEnemigos.Add(novoEnemy);

            // Pasar jugadores conocidos al nuevo soldado
            foreach (Transform jogador in todosJogadores)
            {
                if (jogador != null) novoEnemy.AdicionarJogador(jogador);
            }
        }
    }

    public void RemoverEnemy(EnemyAI enemyMorto)
    {
        if (todosEnemigos.Contains(enemyMorto))
        {
            todosEnemigos.Remove(enemyMorto);
        }
    }

    // --- GESTIÓN DE TANQUES (NUEVO) ---

    public void RegistrarTanque(EnemyTankShooting novoTanque)
    {
        if (!todosTanques.Contains(novoTanque))
        {
            todosTanques.Add(novoTanque);
            Debug.Log($"Tanque registrado: {novoTanque.name}");
        }
    }

    public void RemoverTanque(EnemyTankShooting tanqueMorto)
    {
        if (todosTanques.Contains(tanqueMorto))
        {
            todosTanques.Remove(tanqueMorto);
            Debug.Log($"Tanque removido: {tanqueMorto.name}");
        }
    }

    // --- MÉTODOS DE CONSULTA ---

    public List<Transform> GetTodosJogadores()
    {
        return new List<Transform>(todosJogadores);
    }

    public int GetQuantidadeJogadores()
    {
        return todosJogadores.Count;
    }

    /// <summary>
    /// Devuelve el total de enemigos (Soldados + Tanques)
    /// Útil para saber si la oleada terminó.
    /// </summary>
    public int GetQuantidadeEnemigos()
    {
        return todosEnemigos.Count + todosTanques.Count;
    }

    public Transform GetJogadorMaisProximo(Vector3 posicao)
    {
        if (todosJogadores.Count == 0) return null;

        Transform jogadorMaisProximo = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Transform jogador in todosJogadores)
        {
            if (jogador == null || !jogador.gameObject.activeInHierarchy) continue;

            IHealth health = jogador.GetComponent<IHealth>();
            if (health == null || health.IsDead) continue;

            float distancia = Vector3.Distance(posicao, jogador.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                jogadorMaisProximo = jogador;
            }
        }

        return jogadorMaisProximo;
    }

    public IHealth GetIHealthFromObject(GameObject obj)
    {
        if (obj == null) return null;
        IHealth health = obj.GetComponent<IHealth>();
        if (health != null) return health;
        return obj.GetComponentInChildren<IHealth>();
    }

    public void ReasignarBase(Transform nuevaBase)
    {
        playerBase = nuevaBase;
    }

    public void ForcarAtualizacaoJogadores()
    {
        BuscarYRegistrarTodosJogadores();
    }

    public List<IHealth> GetTodosIHealth()
    {
        List<IHealth> healths = new List<IHealth>();
        foreach (Transform jogador in todosJogadores)
        {
            if (jogador != null)
            {
                IHealth health = jogador.GetComponent<IHealth>();
                if (health != null && !health.IsDead)
                {
                    healths.Add(health);
                }
            }
        }
        return healths;
    }
}