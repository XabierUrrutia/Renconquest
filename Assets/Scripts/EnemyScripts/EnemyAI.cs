using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyShooting))]
public class EnemyAI : MonoBehaviour
{
    [Header("Configuración de Detección")]
    public float alcanceDeteccao = 10f;
    public float distanciaParagemAtaque = 4f;
    public float intervaloChecagem = 0.3f;

    [Header("Sistema de Patrullaje")]
    public bool usarPatrullaje = true;
    public float intervaloPatrullaje = 8f;
    public float duracionParada = 3f;
    public float radioPontosPatrullaje = 10f;
    public float radioMovimientoParada = 2f;

    [Header("Referencias")]
    public Transform baseJogador;

    [Header("Alvos")]
    [Tooltip("Priorizar atacar PlayerBase quando estiver ao alcance")]
    public bool priorizarBaseDoJogador = true;

    private EnemyController movimento;
    private EnemyShooting atirador;
    private Transform jogadorAlvo;

    private List<Transform> jogadoresDisponiveis = new List<Transform>();

    // Control de tempo
    private float proximaChecagemTime = 0f;
    private float ultimoRecalculoPerseguicao = 0f;
    private float ultimaBuscaJogadoresTime = 0f;
    private const float INTERVALO_RECALCULO_PERSEGUICAO = 1f;
    private const float INTERVALO_BUSCA_JOGADORES = 2f;

    // Sistema de patrullaje
    private bool estaPatrullando = false;
    private float tiempoUltimoPatrullaje = 0f;
    private float tiempoInicioParada = 0f;
    private bool enParada = false;
    private Vector3 puntoPatrullajeActual;
    private Vector3 posicionBaseOriginal;
    private Vector3 posicionInicioParada;
    private bool enMovimientoParada = false;
    private Vector3 puntoMovimientoParada;

    // Debug
    public bool debugAtivo = false;

    void Start()
    {
        movimento = GetComponent<EnemyController>();
        atirador = GetComponent<EnemyShooting>();

        BuscarReferenciasIniciais();
        BuscarTodosJogadores();

        if (baseJogador != null)
        {
            posicionBaseOriginal = baseJogador.position;
        }
        else
        {
            posicionBaseOriginal = transform.position;
        }

        // Verificar si estamos en oleada de venganza
        if (EnemyWaveManager.Instance != null && EnemyWaveManager.Instance.IsRevengeWaveActive())
        {
            ModoVenganza();
        }
    }

    void BuscarReferenciasIniciais()
    {
        if (baseJogador == null)
        {
            GameObject baseObj = GameObject.FindGameObjectWithTag("PlayerBase");
            if (baseObj != null)
            {
                baseJogador = baseObj.transform;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Base encontrada: {baseJogador.name}");
            }
        }

        if (baseJogador == null && EnemyManager.Instance != null)
        {
            baseJogador = EnemyManager.Instance.playerBase;
            if (debugAtivo && baseJogador != null)
                Debug.Log($"{gameObject.name}: Base encontrada via EnemyManager: {baseJogador.name}");
        }
    }

    void ModoVenganza()
    {
        // En modo venganza, los enemigos son más agresivos
        usarPatrullaje = false;
        estaPatrullando = false;
        enParada = false;
        enMovimientoParada = false;

        // Aumentar alcance de detección en modo venganza
        alcanceDeteccao *= 1.5f;

        if (debugAtivo) Debug.Log($"{name}: ¡Modo venganza activado!");
    }

    void BuscarTodosJogadores()
    {
        jogadoresDisponiveis.Clear();

        GameObject[] todosJogadores = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject jogadorObj in todosJogadores)
        {
            // CAMBIO: Buscar IHealth en lugar de PlayerHealth específico
            IHealth health = jogadorObj.GetComponent<IHealth>();
            if (health != null && !health.IsDead && jogadorObj.activeInHierarchy)
            {
                jogadoresDisponiveis.Add(jogadorObj.transform);
                if (debugAtivo) Debug.Log($"{gameObject.name}: Unidad encontrada (IHealth): {jogadorObj.name}");
            }
        }

        if (debugAtivo) Debug.Log($"{gameObject.name}: Total de unidades encontradas: {jogadoresDisponiveis.Count}");
    }

    Transform EncontrarJogadorMaisProximo()
    {
        if (jogadoresDisponiveis.Count == 0) return null;

        Transform jogadorMaisProximo = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Transform jogador in jogadoresDisponiveis)
        {
            if (jogador == null || !jogador.gameObject.activeInHierarchy) continue;

            // CAMBIO: Verificar si tiene IHealth y si está vivo
            IHealth health = jogador.GetComponent<IHealth>();
            if (health == null || health.IsDead) continue;

            float distancia = Vector3.Distance(transform.position, jogador.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                jogadorMaisProximo = jogador;
            }
        }

        return jogadorMaisProximo;
    }

    void RemoverJogadoresInativos()
    {
        for (int i = jogadoresDisponiveis.Count - 1; i >= 0; i--)
        {
            Transform jogador = jogadoresDisponiveis[i];
            if (jogador == null || !jogador.gameObject.activeInHierarchy)
            {
                jogadoresDisponiveis.RemoveAt(i);
                continue;
            }

            // CAMBIO: Verificar IHealth
            IHealth health = jogador.GetComponent<IHealth>();
            if (health == null || health.IsDead)
            {
                jogadoresDisponiveis.RemoveAt(i);
            }
        }
    }

    void Update()
    {
        // Verificar si el juego está pausado
        if (Time.timeScale == 0) return;

        // Controlar frecuencia de chequeos para mejorar rendimiento
        if (Time.time < proximaChecagemTime) return;
        proximaChecagemTime = Time.time + intervaloChecagem;

        // Buscar jugadores periódicamente
        if (Time.time - ultimaBuscaJogadoresTime >= INTERVALO_BUSCA_JOGADORES)
        {
            BuscarTodosJogadores();
            ultimaBuscaJogadoresTime = Time.time;
        }
        else
        {
            RemoverJogadoresInativos();
        }

        // Verificar referencias de base
        if (baseJogador == null)
        {
            BuscarReferenciasIniciais();
            if (baseJogador == null) return;
        }

        // Encontrar jugador más cercano
        Transform jogadorProximo = EncontrarJogadorMaisProximo();

        if (jogadorProximo != null)
        {
            float distanciaAoJogador = Vector3.Distance(transform.position, jogadorProximo.position);

            // DEBUG: Mostrar estado atual
            if (debugAtivo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"{gameObject.name} - Distância ao jogador mais próximo: {distanciaAoJogador}, " +
                         $"Alcance: {alcanceDeteccao}, Perseguindo: {EstaPersiguiendoJogador()}");
            }

            // Verificar detección del jugador
            bool jogadorDetectado = distanciaAoJogador <= alcanceDeteccao;

            // Actualizar estado de persecución
            if (jogadorDetectado && !EstaPersiguiendoJogador())
            {
                // Comenzar a perseguir - detener patrullaje
                jogadorAlvo = jogadorProximo;
                estaPatrullando = false;
                enParada = false;
                enMovimientoParada = false;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Começando a perseguir a unidade: {jogadorAlvo.name}");
            }
            else if (!jogadorDetectado && EstaPersiguiendoJogador() && distanciaAoJogador > alcanceDeteccao * 1.5f)
            {
                // Dejar de perseguir (con histeresis para evitar cambios bruscos)
                jogadorAlvo = null;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Parando de perseguir a unidade");
            }
            else if (EstaPersiguiendoJogador() && jogadorAlvo != jogadorProximo && jogadorDetectado)
            {
                // Cambiar a un jugador más cercano si es necesario
                jogadorAlvo = jogadorProximo;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Mudando para unidade mais próxima: {jogadorAlvo.name}");
            }

            // Ejecutar comportamiento según el estado
            if (EstaPersiguiendoJogador() && jogadorAlvo != null)
            {
                PerseguirJogador(jogadorAlvo, distanciaAoJogador);
            }
            else
            {
                GestionarPatrullajeYBase();
            }
        }
        else
        {
            // No hay jugadores disponibles
            if (EstaPersiguiendoJogador())
            {
                jogadorAlvo = null;
            }
            GestionarPatrullajeYBase();
        }
    }

    void GestionarPatrullajeYBase()
    {
        if (usarPatrullaje && !EstaPersiguiendoJogador())
        {
            GestionarPatrullaje();
        }
        else
        {
            IrParaBase();
        }
    }

    void GestionarPatrullaje()
    {
        // Si está en parada, gestionar el movimiento durante la parada
        if (enParada)
        {
            GestionarMovimientoParada();
            return;
        }

        // Si no está patrullando, iniciar patrullaje
        if (!estaPatrullando)
        {
            IniciarPatrullaje();
            return;
        }

        // Verificar si es tiempo de hacer una parada
        if (Time.time - tiempoUltimoPatrullaje >= intervaloPatrullaje)
        {
            IniciarParada();
            return;
        }

        // Continuar patrullando hacia el punto actual
        if (estaPatrullando)
        {
            // Verificar si llegó al punto de patrullaje
            float distanciaAlPunto = Vector3.Distance(transform.position, puntoPatrullajeActual);
            if (distanciaAlPunto < movimento.distanciaParada)
            {
                // Elegir nuevo punto de patrullaje
                GenerarNuevoPuntoPatrullaje();
            }
        }
    }

    void GestionarMovimientoParada()
    {
        // Verificar si la parada ha terminado
        if (Time.time - tiempoInicioParada >= duracionParada)
        {
            FinalizarParada();
            return;
        }

        // Si no está moviéndose durante la parada, iniciar movimiento
        if (!enMovimientoParada)
        {
            IniciarMovimientoParada();
            return;
        }

        // Si está moviéndose durante la parada, verificar si llegó al punto
        if (enMovimientoParada)
        {
            float distanciaAlPunto = Vector3.Distance(transform.position, puntoMovimientoParada);
            if (distanciaAlPunto < movimento.distanciaParada)
            {
                // Elegir nuevo punto de movimiento durante parada
                IniciarMovimientoParada();
            }
        }
    }

    void IniciarPatrullaje()
    {
        estaPatrullando = true;
        tiempoUltimoPatrullaje = Time.time;
        GenerarNuevoPuntoPatrullaje();
        if (debugAtivo) Debug.Log($"{gameObject.name}: Iniciando patrullaje hacia {puntoPatrullajeActual}");
    }

    void GenerarNuevoPuntoPatrullaje()
    {
        // Generar punto aleatorio alrededor de la posición base
        Vector2 puntoAleatorio = Random.insideUnitCircle * radioPontosPatrullaje;
        puntoPatrullajeActual = posicionBaseOriginal + new Vector3(puntoAleatorio.x, puntoAleatorio.y, 0);

        movimento.SetTarget(puntoPatrullajeActual);

        if (debugAtivo) Debug.Log($"{gameObject.name}: Nuevo punto de patrullaje: {puntoPatrullajeActual}");
    }

    void IniciarParada()
    {
        enParada = true;
        estaPatrullando = false;
        enMovimientoParada = false;
        tiempoInicioParada = Time.time;
        posicionInicioParada = transform.position;

        movimento.StopMoving();

        if (debugAtivo) Debug.Log($"{gameObject.name}: Iniciando parada de {duracionParada} segundos con movimiento en radio {radioMovimientoParada}");
    }

    void IniciarMovimientoParada()
    {
        // Generar punto aleatorio dentro del radio de movimiento de parada
        Vector2 puntoAleatorio = Random.insideUnitCircle * radioMovimientoParada;
        puntoMovimientoParada = posicionInicioParada + new Vector3(puntoAleatorio.x, puntoAleatorio.y, 0);

        movimento.SetTarget(puntoMovimientoParada);
        enMovimientoParada = true;

        if (debugAtivo) Debug.Log($"{gameObject.name}: Movimiento durante parada hacia {puntoMovimientoParada}");
    }

    void FinalizarParada()
    {
        enParada = false;
        enMovimientoParada = false;
        tiempoUltimoPatrullaje = Time.time;
        IniciarPatrullaje();

        if (debugAtivo) Debug.Log($"{gameObject.name}: Fin de parada, volviendo al patrullaje normal");
    }

    void PerseguirJogador(Transform jogador, float distanciaAoJogador)
    {
        // Se existir PlayerBase próxima, podes dar prioridade a ela
        if (priorizarBaseDoJogador && baseJogador != null)
        {
            float distBase = Vector2.Distance(transform.position, baseJogador.position);

            // Se a base estiver mais perto que o jogador ou dentro da distância de ataque, focar base
            if (distBase <= distanciaAoJogador || distBase <= distanciaParagemAtaque)
            {
                IrParaBase();
                return;
            }
        }

        if (distanciaAoJogador > distanciaParagemAtaque)
        {
            // Perseguir o jogador - recalcular rota periodicamente
            if (Time.time - ultimoRecalculoPerseguicao >= INTERVALO_RECALCULO_PERSEGUICAO)
            {
                if (debugAtivo) Debug.Log($"{gameObject.name}: Perseguindo jogador para {jogador.position}");
                movimento.SetTarget(jogador.position);
                ultimoRecalculoPerseguicao = Time.time;
            }
        }
        else
        {
            // Parar para disparar ao jogador
            if (debugAtivo) Debug.Log($"{gameObject.name}: Parando para atirar no jogador");
            movimento.StopMoving();
        }
    }

    void IrParaBase()
    {
        if (baseJogador != null)
        {
            float dist = Vector2.Distance(transform.position, baseJogador.position);

            if (dist > distanciaParagemAtaque)
            {
                // Aproximar-se da base
                movimento.SetTarget(baseJogador.position);
                if (debugAtivo && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"{gameObject.name}: Indo para a base em {baseJogador.position}");
                }
            }
            else
            {
                // Dentro da distância de ataque à base: parar para atirar
                movimento.StopMoving();
                if (debugAtivo && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"{gameObject.name}: Parando para atirar na BASE do jogador");
                }
            }
        }
    }

    public void AdicionarJogador(Transform novoJogador)
    {
        if (!jogadoresDisponiveis.Contains(novoJogador))
        {
            // CAMBIO: Verificar IHealth antes de añadir
            IHealth health = novoJogador.GetComponent<IHealth>();
            if (health != null && !health.IsDead)
            {
                jogadoresDisponiveis.Add(novoJogador);
                if (debugAtivo) Debug.Log($"{gameObject.name}: Nueva unidad adicionada: {novoJogador.name}");
            }
        }
    }

    public void RemoverJogador(Transform jogadorMorto)
    {
        if (jogadoresDisponiveis.Contains(jogadorMorto))
        {
            jogadoresDisponiveis.Remove(jogadorMorto);
            if (jogadorAlvo == jogadorMorto)
            {
                jogadorAlvo = null;
            }
            if (debugAtivo) Debug.Log($"{gameObject.name}: Unidad removida: {jogadorMorto.name}");
        }
    }

    public bool EstaPersiguiendoJogador()
    {
        return jogadorAlvo != null;
    }

    public Transform GetJogadorAlvo()
    {
        return jogadorAlvo;
    }

    public void SetUsarPatrullaje(bool usar)
    {
        usarPatrullaje = usar;
        if (!usar)
        {
            estaPatrullando = false;
            enParada = false;
            enMovimientoParada = false;
        }
    }

    public void ActivarModoVenganza()
    {
        ModoVenganza();
    }

    // ADIÇÃO: helpers para tratar PlayerBase como alvo válido
    public bool EstaAlvejandoBase()
    {
        return jogadorAlvo != null && jogadorAlvo.GetComponent<PlayerBase>() != null;
    }

    void OnDestroy()
    {
        // Desregistrar com EnemyWaveManager
        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.UnregisterEnemy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar alcance de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceDeteccao);

        // Dibujar distancia de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaParagemAtaque);

        // Dibujar línea al jugador si está siendo perseguido
        if (Application.isPlaying && EstaPersiguiendoJogador() && jogadorAlvo != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, jogadorAlvo.position);
        }

        // Dibujar líneas a todos los jugadores detectados
        Gizmos.color = Color.blue;
        foreach (Transform jogador in jogadoresDisponiveis)
        {
            if (jogador != null && jogador != jogadorAlvo)
            {
                Gizmos.DrawLine(transform.position, jogador.position);
            }
        }

        // Dibujar información de patrullaje
        if (usarPatrullaje)
        {
            // Área de patrullaje
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawWireSphere(posicionBaseOriginal, radioPontosPatrullaje);

            // Punto actual de patrullaje
            if (Application.isPlaying && estaPatrullando)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(puntoPatrullajeActual, 0.5f);
                Gizmos.DrawLine(transform.position, puntoPatrullajeActual);
            }

            // Información de movimiento durante parada
            if (enParada)
            {
                // Radio de movimiento durante parada
                Gizmos.color = new Color(1, 0, 1, 0.3f);
                Gizmos.DrawWireSphere(posicionInicioParada, radioMovimientoParada);

                // Punto de movimiento durante parada
                if (enMovimientoParada)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(puntoMovimientoParada, 0.3f);
                    Gizmos.DrawLine(transform.position, puntoMovimientoParada);
                }

                // Indicador visual de parada
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, 0.8f);
            }
        }

        // Dibujar línea a la base si no está persiguiendo
        if (Application.isPlaying && !EstaPersiguiendoJogador() && baseJogador != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, baseJogador.position);
        }
    }
}