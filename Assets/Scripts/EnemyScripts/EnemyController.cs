using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movimiento Básico")]
    public float velocidad = 4f;
    public float distanciaParada = 0.1f;

    [Header("Búsqueda de Puentes")]
    public float radioBusquedaMaximo = 50f;
    public int maxPuentesEnRuta = 5;

    [Header("Detección de Terreno")]
    public LayerMask capaSuelo;
    public LayerMask capaAgua;
    public LayerMask capaWaypointPuente;

    [Header("Sprites - 8 direcciones (2 frames + idle)")]
    public Sprite frenteDerecha_L;
    public Sprite frenteDerecha_R;
    public Sprite frenteDerecha_Idle;

    public Sprite frenteIzquierda_L;
    public Sprite frenteIzquierda_R;
    public Sprite frenteIzquierda_Idle;

    public Sprite atrasDerecha_L;
    public Sprite atrasDerecha_R;
    public Sprite atrasDerecha_Idle;

    public Sprite atrasIzquierda_L;
    public Sprite atrasIzquierda_R;
    public Sprite atrasIzquierda_Idle;

    // Variables internas
    private Vector3 objetivoFinal;
    private bool moviendose = false;
    private List<Vector3> puntosCamino = new List<Vector3>();
    private Vector2 direccionMovimiento;
    private SpriteRenderer spriteRenderer;

    // Animación
    private float temporizadorAnim = 0f;
    private bool alternarAnim = false;
    private Vector2 ultimaDireccion = new Vector2(1, -1);

    // --- NUEVO: Variable para bloquear la animación ---
    [HideInInspector] public bool bloquearAnimacion = false;
    // --------------------------------------------------

    // Control de pathfinding
    private float ultimoRecalculoTime = 0f;
    private const float RECALCULO_INTERVALO = 2f; // Recalcular cada 2 segundos máximo

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        objetivoFinal = transform.position;
        ActualizarSprite(ultimaDireccion, false, true);
    }

    void Update()
    {
        Mover();
        ActualizarAnimacion();
    }

    public void SetTarget(Vector3 posicionDestino)
    {
        // Solo recalcular si el objetivo cambió significativamente o ha pasado suficiente tiempo
        if (Vector3.Distance(objetivoFinal, posicionDestino) > 0.5f ||
            Time.time - ultimoRecalculoTime > RECALCULO_INTERVALO)
        {
            objetivoFinal = posicionDestino;
            ultimoRecalculoTime = Time.time;

            if (EsSueloValido(posicionDestino))
            {
                CalcularRutaCompleta(transform.position, posicionDestino);
            }
            else
            {
                Vector3 puntoCercano = EncontrarPuntoCercanoValido(posicionDestino);
                if (puntoCercano != Vector3.zero)
                {
                    CalcularRutaCompleta(transform.position, puntoCercano);
                }
            }
        }
    }

    public void StopMoving()
    {
        moviendose = false;
        puntosCamino.Clear();
    }

    void CalcularRutaCompleta(Vector3 inicio, Vector3 destino)
    {
        puntosCamino.Clear();
        moviendose = false;

        // PRIMERO: Verificar si el camino directo es posible (sin agua)
        if (!CaminoTieneAgua(inicio, destino))
        {
            puntosCamino.Add(destino);
            moviendose = true;
            return;
        }

        // Buscar ruta completa a través de puentes
        List<Vector3> rutaCompleta = EncontrarRutaConPuentes(inicio, destino, maxPuentesEnRuta);

        if (rutaCompleta != null && rutaCompleta.Count > 0)
        {
            puntosCamino.AddRange(rutaCompleta);
            moviendose = true;
        }
        else
        {
            // Intentar al menos llegar a algún puente
            IntentarRutaParcial(inicio, destino);
        }
    }

    List<Vector3> EncontrarRutaConPuentes(Vector3 inicio, Vector3 destino, int maxProfundidad)
    {
        Queue<List<Vector3>> colaRutas = new Queue<List<Vector3>>();
        HashSet<WaypointPuente> visitados = new HashSet<WaypointPuente>();

        List<Vector3> rutaInicial = new List<Vector3> { inicio };
        colaRutas.Enqueue(rutaInicial);

        int iteraciones = 0;
        const int MAX_ITERACIONES = 100; // Límite para evitar bucles infinitos

        while (colaRutas.Count > 0 && iteraciones < MAX_ITERACIONES)
        {
            iteraciones++;

            List<Vector3> rutaActual = colaRutas.Dequeue();
            Vector3 posicionActual = rutaActual[rutaActual.Count - 1];

            // Si hemos alcanzado el destino, retornar la ruta
            if (Vector3.Distance(posicionActual, destino) < 1f)
            {
                rutaActual.RemoveAt(0);
                return rutaActual;
            }

            // Si la ruta es demasiado larga, continuar
            if (rutaActual.Count > maxProfundidad * 2 + 2) continue;

            // Buscar todos los puentes alcanzables desde la posición actual
            List<WaypointPuente> puentesAlcanzables = EncontrarPuentesAlcanzables(posicionActual, visitados);

            foreach (WaypointPuente puente in puentesAlcanzables)
            {
                // Verificar que podemos llegar al puente y que el puente tiene conexión
                if (!CaminoTieneAgua(posicionActual, puente.transform.position) &&
                    puente.waypointConectado != null)
                {
                    // Crear nueva ruta
                    List<Vector3> nuevaRuta = new List<Vector3>(rutaActual);
                    nuevaRuta.Add(puente.transform.position);
                    nuevaRuta.Add(puente.waypointConectado.transform.position);

                    // Marcar como visitado
                    visitados.Add(puente);

                    // Añadir a la cola
                    colaRutas.Enqueue(nuevaRuta);
                }
            }

            // También considerar ir directo al destino si es posible
            if (!CaminoTieneAgua(posicionActual, destino))
            {
                List<Vector3> rutaDirecta = new List<Vector3>(rutaActual);
                rutaDirecta.Add(destino);
                colaRutas.Enqueue(rutaDirecta);
            }
        }

        return null;
    }

    List<WaypointPuente> EncontrarPuentesAlcanzables(Vector3 desde, HashSet<WaypointPuente> excluir)
    {
        List<WaypointPuente> puentes = new List<WaypointPuente>();

        // Usar OverlapCircleNonAlloc para mejor rendimiento
        Collider2D[] colliders = new Collider2D[20];
        int count = Physics2D.OverlapCircleNonAlloc(desde, radioBusquedaMaximo, colliders, capaWaypointPuente);

        for (int i = 0; i < count; i++)
        {
            WaypointPuente puente = colliders[i].GetComponent<WaypointPuente>();
            if (puente != null && puente.waypointConectado != null && !excluir.Contains(puente))
            {
                // Verificar que el puente esté en dirección general al destino
                Vector3 direccionAlPuente = (puente.transform.position - desde).normalized;
                Vector3 direccionAlDestino = (objetivoFinal - desde).normalized;
                float similitud = Vector3.Dot(direccionAlPuente, direccionAlDestino);

                if (similitud > -0.3f) // Permitir cierta desviación
                {
                    puentes.Add(puente);
                }
            }
        }

        // Ordenar por proximidad al destino final
        return puentes.OrderBy(p => Vector3.Distance(p.waypointConectado.transform.position, objetivoFinal)).ToList();
    }

    void IntentarRutaParcial(Vector3 inicio, Vector3 destino)
    {
        // Encontrar el puente que nos lleve más cerca del destino
        WaypointPuente mejorPuente = null;
        float mejorDistancia = Mathf.Infinity;

        // Usar OverlapCircleNonAlloc para mejor rendimiento
        Collider2D[] todosPuentes = new Collider2D[20];
        int count = Physics2D.OverlapCircleNonAlloc(inicio, radioBusquedaMaximo, todosPuentes, capaWaypointPuente);

        for (int i = 0; i < count; i++)
        {
            WaypointPuente puente = todosPuentes[i].GetComponent<WaypointPuente>();
            if (puente != null && puente.waypointConectado != null)
            {
                if (!CaminoTieneAgua(inicio, puente.transform.position))
                {
                    float distanciaDesdePuente = Vector3.Distance(puente.waypointConectado.transform.position, destino);
                    if (distanciaDesdePuente < mejorDistancia)
                    {
                        mejorDistancia = distanciaDesdePuente;
                        mejorPuente = puente;
                    }
                }
            }
        }

        if (mejorPuente != null)
        {
            puntosCamino.Add(mejorPuente.transform.position);
            puntosCamino.Add(mejorPuente.waypointConectado.transform.position);
            moviendose = true;
        }
    }

    void Mover()
    {
        if (!moviendose || puntosCamino.Count == 0) return;

        Vector3 objetivoActual = puntosCamino[0];
        Vector3 direccion = (objetivoActual - transform.position).normalized;

        // Mover hacia el objetivo
        transform.position += direccion * velocidad * Time.deltaTime;
        direccionMovimiento = direccion;

        // Verificar si llegamos al punto actual
        if (Vector3.Distance(transform.position, objetivoActual) < distanciaParada)
        {
            puntosCamino.RemoveAt(0);

            if (puntosCamino.Count == 0)
            {
                moviendose = false;

                // Verificar si realmente llegamos al objetivo final
                if (Vector3.Distance(transform.position, objetivoFinal) > 1f)
                {
                    // Recalcular solo si ha pasado suficiente tiempo
                    if (Time.time - ultimoRecalculoTime > RECALCULO_INTERVALO)
                    {
                        CalcularRutaCompleta(transform.position, objetivoFinal);
                    }
                }
            }
        }
    }

    Vector3 EncontrarPuntoCercanoValido(Vector3 destino)
    {
        // Buscar en círculos concéntricos alrededor del destino
        float radio = 1f;
        int maxIntentos = 5;

        for (int i = 0; i < maxIntentos; i++)
        {
            for (int j = 0; j < 8; j++) // 8 direcciones
            {
                float angulo = j * 45f * Mathf.Deg2Rad;
                Vector3 puntoPrueba = destino + new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0) * radio;

                if (EsSueloValido(puntoPrueba) && !CaminoTieneAgua(transform.position, puntoPrueba))
                {
                    return puntoPrueba;
                }
            }
            radio += 2f;
        }

        return Vector3.zero;
    }

    void ActualizarAnimacion()
    {
        // --- CAMBIO CLAVE: Si está bloqueado, no tocamos el sprite ---
        if (bloquearAnimacion) return;
        // -------------------------------------------------------------

        if (moviendose && direccionMovimiento.magnitude > 0.1f)
        {
            temporizadorAnim += Time.deltaTime;
            if (temporizadorAnim >= 0.2f)
            {
                temporizadorAnim = 0f;
                alternarAnim = !alternarAnim;
            }

            ActualizarSprite(direccionMovimiento, alternarAnim);
            ultimaDireccion = direccionMovimiento;
        }
        else
        {
            ActualizarSprite(ultimaDireccion, false, true);
        }
    }

    void ActualizarSprite(Vector2 direccion, bool alternar, bool idle = false)
    {
        if (spriteRenderer == null) return;

        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        if (angulo < 0) angulo += 360;

        Sprite spriteSeleccionado = frenteDerecha_Idle;

        // Distribución de direcciones
        if (angulo >= 337.5f || angulo < 22.5f)        // Derecha
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 22.5f && angulo < 67.5f)    // Arriba-Derecha
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 67.5f && angulo < 112.5f)   // Arriba
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 112.5f && angulo < 157.5f)  // Arriba-Izquierda
            spriteSeleccionado = idle ? atrasIzquierda_Idle : (alternar ? atrasIzquierda_L : atrasIzquierda_R);
        else if (angulo >= 157.5f && angulo < 202.5f)  // Izquierda
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 202.5f && angulo < 247.5f)  // Abajo-Izquierda
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 247.5f && angulo < 292.5f)  // Abajo
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 292.5f && angulo < 337.5f)  // Abajo-Derecha
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);

        spriteRenderer.sprite = spriteSeleccionado;
    }

    bool CaminoTieneAgua(Vector3 inicio, Vector3 fin)
    {
        float distancia = Vector3.Distance(inicio, fin);
        if (distancia < 0.1f) return false;

        int muestras = Mathf.CeilToInt(distancia / 0.3f);

        for (int i = 0; i <= muestras; i++)
        {
            float t = (float)i / (float)muestras;
            Vector3 punto = Vector3.Lerp(inicio, fin, t);

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
        return Physics2D.OverlapCircle(posicion, 0.3f, capaSuelo) != null;
    }

    // Para debug visual (solo en el editor)
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        // Dibujar camino planeado
        if (puntosCamino != null && puntosCamino.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 anterior = transform.position;

            foreach (Vector3 punto in puntosCamino)
            {
                Gizmos.DrawWireSphere(punto, 0.2f);
                Gizmos.DrawLine(anterior, punto);
                anterior = punto;
            }
        }

        // Dibujar dirección actual
        if (moviendose)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direccionMovimiento * 1f);
        }

        // Dibujar área de búsqueda de waypoints
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioBusquedaMaximo);

        // Dibujar objetivo final
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(objetivoFinal, 0.5f);
#endif
    }
}