using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SelectableUnit))]
public class TankMovement : MonoBehaviour, ISelectableUnit
{
    [Header("Referencias Visuales")]
    public TankVisuals tankVisuals;
    public GameObject prefabMarcadorClick;

    [Header("Configuraci�n de Movimiento")]
    public float velocidad = 3f;
    public float distanciaParada = 0.1f;

    [Header("Detecci�n de Terreno (Capas)")]
    public LayerMask capaSuelo;
    public LayerMask capaAgua;
    public LayerMask capaWaypointPuente;

    // Referencias internas
    private Rigidbody2D rb;
    private SelectableUnit selectableUnitComponent;

    // Variables de Estado y Ruta
    private List<Vector3> puntosCamino = new List<Vector3>();
    private bool moviendose = false;
    private bool estaSeleccionado = false;

    void Start()
    {
        // Configuraci�n f�sica para movimiento Top-Down
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.freezeRotation = true;

        // Obtener componentes autom�ticamente si no se asignaron
        if (tankVisuals == null) tankVisuals = GetComponent<TankVisuals>();
        selectableUnitComponent = GetComponent<SelectableUnit>();
    }

    void Update()
    {
        Mover();
    }

    // =========================================================
    //              INTERFAZ ISelectableUnit
    // =========================================================

    public void Seleccionar()
    {
        estaSeleccionado = true;
        if (selectableUnitComponent != null) selectableUnitComponent.ShowSelection(true);
    }

    public void Deseleccionar()
    {
        estaSeleccionado = false;
        if (selectableUnitComponent != null) selectableUnitComponent.ShowSelection(false);
    }

    // =========================================================
    //              SISTEMA DE INPUT Y RUTA
    // =========================================================

    public void MoverADestino(Vector3 destino)
    {
        // Solo obedecemos si estamos seleccionados
        if (estaSeleccionado)
        {
            Vector3 posRaton = new Vector3(destino.x, destino.y, 0);

            // 1. Validamos que el click sea en tierra firme o un puente
            if (EsSueloValido(posRaton))
            {
                // 2. Calculamos la ruta evitando el agua
                CalcularRutaInteligente(transform.position, posRaton);

                // 3. Efecto visual del click
                if (prefabMarcadorClick != null)
                {
                    GameObject marcador = Instantiate(prefabMarcadorClick, posRaton, Quaternion.identity);
                    Destroy(marcador, 1f);
                }
            }
            else
            {
                Debug.Log("Orden inv�lida: Destino es agua o vac�o.");
            }
        }
    }

    void CalcularRutaInteligente(Vector3 inicio, Vector3 destino)
    {
        puntosCamino.Clear();
        moviendose = false;

        // PASO 1: Comprobar si hay agua en l�nea recta
        if (!CaminoTieneAgua(inicio, destino))
        {
            // Camino libre: Vamos directo
            puntosCamino.Add(destino);
            moviendose = true;
        }
        else
        {
            // PASO 2: Hay agua. Buscar un puente cercano.
            WaypointPuente puenteUtil = EncontrarPuenteSimple(inicio, destino);

            if (puenteUtil != null && puenteUtil.waypointConectado != null)
            {
                // Comprobamos que podemos llegar al puente sin mojarnos
                if (!CaminoTieneAgua(inicio, puenteUtil.transform.position))
                {
                    // Ruta: Inicio -> Entrada Puente -> Salida Puente -> Destino
                    puntosCamino.Add(puenteUtil.transform.position);
                    puntosCamino.Add(puenteUtil.waypointConectado.transform.position);
                    puntosCamino.Add(destino);
                    moviendose = true;
                }
            }
        }
    }

    // =========================================================
    //              L�GICA DE MOVIMIENTO F�SICO
    // =========================================================

    void Mover()
    {
        if (!moviendose || puntosCamino.Count == 0)
        {
            rb.linearVelocity = Vector2.zero;
            if (tankVisuals) tankVisuals.ActualizarVisuales(Vector2.zero, false);
            return;
        }

        Vector3 objetivo = puntosCamino[0];
        Vector3 dirVector = (objetivo - transform.position);

        // Calcular direcci�n normalizada
        Vector2 direccion = Vector2.zero;
        if (dirVector.magnitude > 0.01f)
        {
            direccion = new Vector2(dirVector.x, dirVector.y).normalized;
        }

        // Mover el tanque
        transform.position += (Vector3)direccion * velocidad * Time.deltaTime;

        // Actualizar visuales (rotaci�n de orugas/torreta)
        if (tankVisuals != null)
        {
            tankVisuals.ActualizarVisuales(direccion, true);
        }

        // Comprobar si llegamos al punto actual
        if (Vector2.Distance(transform.position, objetivo) < distanciaParada)
        {
            puntosCamino.RemoveAt(0); // Siguiente punto
            if (puntosCamino.Count == 0) moviendose = false; // Fin de ruta
        }
    }

    // =========================================================
    //              UTILIDADES DE DETECCI�N (EL FIX)
    // =========================================================

    // Comprueba paso a paso si una l�nea cruza la capa de agua
    bool CaminoTieneAgua(Vector3 inicio, Vector3 fin)
    {
        float distancia = Vector3.Distance(inicio, fin);
        if (distancia < 0.1f) return false;

        // Cuantas m�s muestras, m�s precisa es la detecci�n (cada 0.5 metros)
        int muestras = Mathf.CeilToInt(distancia / 0.5f);

        for (int i = 0; i <= muestras; i++)
        {
            float t = (float)i / (float)muestras;
            Vector3 punto = Vector3.Lerp(inicio, fin, t);

            // Si detectamos AGUA y NO estamos sobre un PUENTE -> Es impasable
            bool tocaAgua = Physics2D.OverlapCircle(punto, 0.4f, capaAgua);
            bool tocaPuente = Physics2D.OverlapCircle(punto, 0.4f, capaWaypointPuente);

            if (tocaAgua && !tocaPuente)
            {
                return true; // Camino bloqueado
            }
        }
        return false;
    }

    WaypointPuente EncontrarPuenteSimple(Vector3 inicio, Vector3 destino)
    {
        // Busca puentes en un radio de 20 metros
        Collider2D[] todosWaypoints = Physics2D.OverlapCircleAll(inicio, 20f, capaWaypointPuente);
        WaypointPuente mejorPuente = null;
        float menorDistanciaTotal = Mathf.Infinity;

        foreach (Collider2D collider in todosWaypoints)
        {
            WaypointPuente waypoint = collider.GetComponent<WaypointPuente>();

            // Verificamos que sea una entrada v�lida y tenga salida conectada
            if (waypoint != null && waypoint.waypointConectado != null)
            {
                // Calculamos la distancia total: Inicio->Puente + Salida->Destino
                float distA = Vector3.Distance(inicio, waypoint.transform.position);
                float distB = Vector3.Distance(waypoint.waypointConectado.transform.position, destino);
                float distanciaTotal = distA + distB;

                if (distanciaTotal < menorDistanciaTotal)
                {
                    // �ltima comprobaci�n: �Podemos llegar a la entrada del puente sin agua?
                    if (!CaminoTieneAgua(inicio, waypoint.transform.position))
                    {
                        menorDistanciaTotal = distanciaTotal;
                        mejorPuente = waypoint;
                    }
                }
            }
        }
        return mejorPuente;
    }

    bool EsSueloValido(Vector3 pos)
    {
        // Un click es v�lido si es Suelo O Puente
        bool enSuelo = Physics2D.OverlapCircle(pos, 0.4f, capaSuelo) != null;
        bool enPuente = Physics2D.OverlapCircle(pos, 0.4f, capaWaypointPuente) != null;

        return enSuelo || enPuente;
    }
}