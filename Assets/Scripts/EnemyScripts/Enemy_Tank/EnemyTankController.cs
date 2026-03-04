using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(TankVisuals))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyTankController : MonoBehaviour
{
    [Header("Referencias Visuales")]
    public TankVisuals tankVisuals;

    [Header("Movimiento B�sico")]
    public float velocidad = 2.5f; // Los tanques suelen ser un poco m�s lentos
    public float distanciaParada = 0.5f;

    [Header("B�squeda de Puentes")]
    public float radioBusquedaMaximo = 50f;
    public int maxPuentesEnRuta = 5;

    [Header("Detecci�n de Terreno")]
    public LayerMask capaSuelo;
    public LayerMask capaAgua;
    public LayerMask capaWaypointPuente;

    // Variables internas de estado
    private Vector3 objetivoFinal;
    private bool moviendose = false;
    private List<Vector3> puntosCamino = new List<Vector3>();
    private Vector2 direccionMovimiento;
    private Rigidbody2D rb;

    // Control de optimizaci�n
    private float ultimoRecalculoTime = 0f;
    private const float RECALCULO_INTERVALO = 1.0f;

    // Propiedades p�blicas para otros scripts
    public bool IsMoving => moviendose;
    public Vector3 TargetPosition => objetivoFinal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        if (tankVisuals == null) tankVisuals = GetComponent<TankVisuals>();

        objetivoFinal = transform.position;
    }

    void Update()
    {
        Mover();
        ActualizarAnimacion();
    }

    // ---------------------------------------------------------
    // L�GICA DE MOVIMIENTO
    // ---------------------------------------------------------

    public void SetTarget(Vector3 posicionDestino)
    {
        // Debug para ver qu� est� pasando
        Debug.DrawLine(transform.position, posicionDestino, Color.yellow);

        if (Vector3.Distance(objetivoFinal, posicionDestino) > 1.0f ||
            Time.time - ultimoRecalculoTime > RECALCULO_INTERVALO)
        {
            objetivoFinal = posicionDestino;
            ultimoRecalculoTime = Time.time;

            // VERIFICACI�N 1: �Es suelo v�lido?
            if (EsSueloValido(posicionDestino))
            {
                // VERIFICACI�N 2: �Hay agua?
                if (CaminoTieneAgua(transform.position, posicionDestino))
                {
                    Debug.Log("Tanque: Detecto agua, buscando puente...");
                    EncontrarRutaConPuentes(posicionDestino);
                }
                else
                {
                    // Camino despejado
                    puntosCamino.Clear();
                    puntosCamino.Add(posicionDestino);
                    moviendose = true;
                    // Debug.Log("Tanque: Camino directo encontrado. Moviendo.");
                }
            }
            else
            {
                Debug.LogError($"Tanque: El destino {posicionDestino} NO se detecta como 'Capa Suelo'. Revisa los Colliders del suelo.");

                // Intento de recuperaci�n
                Vector3 puntoCercano = EncontrarPuntoCercanoValido(posicionDestino);
                if (puntoCercano != Vector3.zero)
                {
                    SetTarget(puntoCercano);
                }
            }
        }
    }

    public void StopMoving()
    {
        moviendose = false;
        puntosCamino.Clear();
        rb.linearVelocity = Vector2.zero;
    }

    void Mover()
    {
        if (!moviendose || puntosCamino.Count == 0)
        {
            moviendose = false;
            direccionMovimiento = Vector2.zero;
            return;
        }

        Vector3 objetivoActual = puntosCamino[0];

        // Calcular direcci�n
        Vector3 dir3 = (objetivoActual - transform.position).normalized;
        direccionMovimiento = new Vector2(dir3.x, dir3.y);

        // Mover
        transform.position += dir3 * velocidad * Time.deltaTime;

        // Chequear llegada al punto actual
        if (Vector3.Distance(transform.position, objetivoActual) < distanciaParada)
        {
            puntosCamino.RemoveAt(0);

            if (puntosCamino.Count == 0)
            {
                moviendose = false;
                direccionMovimiento = Vector2.zero;
            }
        }
    }

    // ---------------------------------------------------------
    // CONEXI�N CON TANK VISUALS (�LA CLAVE!)
    // ---------------------------------------------------------
    void ActualizarAnimacion()
    {
        if (tankVisuals != null)
        {
            // Enviamos la direcci�n y si nos estamos moviendo
            // TankVisuals se encarga de calcular los �ngulos (0, 90, 180, 270)
            tankVisuals.ActualizarVisuales(direccionMovimiento, moviendose);
        }
    }

    // ---------------------------------------------------------
    // L�GICA DE PATHFINDING Y PUENTES (Replicada de tu c�digo)
    // ---------------------------------------------------------

    void EncontrarRutaConPuentes(Vector3 destinoFinal)
    {
        puntosCamino.Clear();
        Vector3 puntoActual = transform.position;

        // Simulaci�n simplificada de b�squeda de puentes (Greedy)
        for (int i = 0; i < maxPuentesEnRuta; i++)
        {
            if (!CaminoTieneAgua(puntoActual, destinoFinal))
            {
                puntosCamino.Add(destinoFinal);
                moviendose = true;
                return;
            }

            // Buscar puentes cercanos
            Collider2D[] puentes = Physics2D.OverlapCircleAll(puntoActual, radioBusquedaMaximo, capaWaypointPuente);

            Collider2D mejorPuente = null;
            float menorDistancia = float.MaxValue;

            foreach (var puente in puentes)
            {
                // Filtrar puentes que nos acerquen al destino y sean alcanzables
                float distAObjetivo = Vector3.Distance(puente.transform.position, destinoFinal);
                if (distAObjetivo < Vector3.Distance(puntoActual, destinoFinal))
                {
                    if (!CaminoTieneAgua(puntoActual, puente.transform.position))
                    {
                        if (distAObjetivo < menorDistancia)
                        {
                            menorDistancia = distAObjetivo;
                            mejorPuente = puente;
                        }
                    }
                }
            }

            if (mejorPuente != null)
            {
                puntosCamino.Add(mejorPuente.transform.position);
                puntoActual = mejorPuente.transform.position;
            }
            else
            {
                // No hay camino claro
                break;
            }
        }

        // Si encontramos al menos un punto, nos movemos
        if (puntosCamino.Count > 0) moviendose = true;
    }

    // Comprueba si hay agua entre dos puntos lanzando un Raycast o muestras
    bool CaminoTieneAgua(Vector3 inicio, Vector3 fin)
    {
        float distancia = Vector3.Distance(inicio, fin);
        int muestras = Mathf.CeilToInt(distancia * 2); // Una muestra cada 0.5 unidades

        for (int i = 1; i <= muestras; i++)
        {
            float t = (float)i / muestras;
            Vector3 punto = Vector3.Lerp(inicio, fin, t);

            // Si tocamos agua y NO estamos sobre un puente
            if (Physics2D.OverlapCircle(punto, 0.2f, capaAgua) &&
                !Physics2D.OverlapCircle(punto, 0.2f, capaWaypointPuente))
            {
                return true;
            }
        }
        return false;
    }

    bool EsSueloValido(Vector3 posicion)
    {
        // Debe haber suelo y no ser obst�culo
        return Physics2D.OverlapCircle(posicion, 0.3f, capaSuelo) != null;
    }

    Vector3 EncontrarPuntoCercanoValido(Vector3 destino)
    {
        // B�squeda simple en espiral o aleatoria alrededor del punto inv�lido
        for (float r = 1f; r < 5f; r += 1f)
        {
            for (int angle = 0; angle < 360; angle += 45)
            {
                Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.right * r;
                Vector3 checkPos = destino + offset;
                if (EsSueloValido(checkPos) && !CaminoTieneAgua(transform.position, checkPos))
                {
                    return checkPos;
                }
            }
        }
        return Vector3.zero;
    }

    // Debug visual en el editor
    void OnDrawGizmosSelected()
    {
        if (puntosCamino.Count > 0)
        {
            Gizmos.color = Color.green;
            Vector3 prev = transform.position;
            foreach (var p in puntosCamino)
            {
                Gizmos.DrawLine(prev, p);
                Gizmos.DrawWireSphere(p, 0.3f);
                prev = p;
            }
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radioBusquedaMaximo);
    }


}