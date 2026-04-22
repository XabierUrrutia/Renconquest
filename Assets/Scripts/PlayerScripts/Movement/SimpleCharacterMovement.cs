using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SelectableUnit))]
public class SimpleCharacterMovement : MonoBehaviour, ISelectableUnit
{
    [Header("Movimiento Básico")]
    public float velocidad = 4f;
    public float distanciaParada = 0.15f;

    [Header("Cooldown")]
    public float cooldownClick = 1.5f;
    private float ultimoClickTime;
    private bool puedeClickar = true;

    [Header("Detección de Terreno")]
    public LayerMask capaSuelo;
    public LayerMask capaAgua;
    public LayerMask capaWaypointPuente;

    [Header("Waypoints de Navegación")]
    [Tooltip("Layer donde están los WaypointNodo")]
    public LayerMask capaWaypoints;
    [Tooltip("Radio de búsqueda de waypoints cuando hay un obstáculo")]
    public float radioBusquedaWaypoints = 8f;

    [Header("Radio búsqueda puente")]
    public float radioBusquedaPuente = 20f;

    [Header("Sprites - 8 direcciones")]
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

    [Header("Marcador de Click")]
    public GameObject prefabMarcadorClick;

    private SelectableUnit selectableUnitComponent;
    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private SpriteRenderer spriteRenderer;

    private bool moviendose = false;
    private List<Vector3> puntosCamino = new List<Vector3>();
    private Vector2 direccionMovimiento;

    private float temporizadorAnim = 0f;
    private bool alternarAnim = false;
    private Vector2 ultimaDireccion = new Vector2(1, -1);
    private bool estaSeleccionado = false;

    private Vector2 ultimaPosicion;
    private float tiempoAtascado = 0f;
    private const float DISTANCIA_MINIMA_MOVIMIENTO = 0.02f;
    private const float TIEMPO_CANCELAR = 1.5f;

    private float tiempoUltimoChequeo = 0f;
    private const float INTERVALO_CHEQUEO_RUTA = 0.1f;

    private float tiempoUltimoRecalculo = 0f;
    private const float COOLDOWN_RECALCULO = 0.5f;

    private int capEdificios;
    private readonly Collider2D[] _bufferOverlap = new Collider2D[16];

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        selectableUnitComponent = GetComponent<SelectableUnit>();
        capEdificios = LayerMask.GetMask("Buildings");
        ActualizarSprite(ultimaDireccion, false, true);
    }

    void Update()
    {
        ActualizarCooldown();
        ActualizarAnimacion();
    }

    void FixedUpdate()
    {
        ChequearRutaPeriodica();
        Mover();
    }

    public void Seleccionar()
    {
        estaSeleccionado = true;
        if (selectableUnitComponent != null)
            selectableUnitComponent.ShowSelection(true);
    }

    public void Deseleccionar()
    {
        estaSeleccionado = false;
        if (selectableUnitComponent != null)
            selectableUnitComponent.ShowSelection(false);
    }

    public void Detener()
    {
        puntosCamino.Clear();
        moviendose = false;
        tiempoAtascado = 0f;
    }

    public void MoverADestino(Vector3 destino)
    {
        if (!puedeClickar || !estaSeleccionado) return;

        Vector3 pos = new Vector3(destino.x, destino.y, 0);
        if (!EsSueloValido(pos)) return;

        ConfigurarCooldown();
        CalcularRutaInteligente(transform.position, pos);
        tiempoUltimoChequeo = INTERVALO_CHEQUEO_RUTA; // fuerza chequeo en el próximo FixedUpdate
    }

    void CalcularRutaInteligente(Vector3 inicio, Vector3 destino)
    {
        puntosCamino.Clear();
        moviendose = false;
        tiempoAtascado = 0f;

        // Si el punto de inicio está dentro de un edificio, buscar el waypoint más cercano para salir primero
        Collider2D dentroDe = Physics2D.OverlapCapsule((Vector2)inicio + col.offset, col.size, col.direction, 0f, capEdificios);
        if (dentroDe != null)
        {
            int n = Physics2D.OverlapCircleNonAlloc(inicio, radioBusquedaWaypoints * 2f, _bufferOverlap, capaWaypoints);
            Vector3 mejorSalida = Vector3.zero;
            float menorDist = Mathf.Infinity;
            for (int i = 0; i < n; i++)
            {
                float d = Vector2.Distance(inicio, _bufferOverlap[i].transform.position);
                if (d < menorDist)
                {
                    menorDist = d;
                    mejorSalida = _bufferOverlap[i].transform.position;
                }
            }
            if (mejorSalida != Vector3.zero)
            {
                puntosCamino.Add(mejorSalida);
                inicio = mejorSalida;
            }
        }

        if (CaminoTieneAgua(inicio, destino))
        {
            WaypointPuente puente = EncontrarMejorPuente(inicio, destino);
            if (puente != null && puente.waypointConectado != null)
            {
                AñadirRutaConWaypoints(inicio, puente.transform.position);
                puntosCamino.Add(puente.waypointConectado.transform.position);
                AñadirRutaConWaypoints(puente.waypointConectado.transform.position, destino);
            }
            else
            {
                AñadirRutaConWaypoints(inicio, destino);
            }
        }
        else
        {
            AñadirRutaConWaypoints(inicio, destino);
        }

        moviendose = puntosCamino.Count > 0;
    }

    void AñadirRutaConWaypoints(Vector3 inicio, Vector3 destino, int profundidad = 0)
    {
        if (profundidad > 5)
        {
            puntosCamino.Add(destino);
            return;
        }

        Vector2 dir = ((Vector2)destino - (Vector2)inicio).normalized;
        float distancia = Vector2.Distance(inicio, destino);

        // Empezamos el cast un poco adelantado para no auto-detectarnos
        Vector2 origenCast = (Vector2)inicio + col.offset + dir * 0.1f;
        RaycastHit2D hit = Physics2D.CapsuleCast(
            origenCast,
            col.size * 1.1f,  // un poco más grande para margen de seguridad
            col.direction, 0f,
            dir, distancia,
            capEdificios);

        if (hit.collider == null)
        {
            puntosCamino.Add(destino);
            return;
        }

        // Obstáculo detectado — buscar waypoint de rodeo
        Vector3 waypointRodeo = EncontrarWaypointRodeo(inicio, destino, hit.collider);

        if (waypointRodeo == Vector3.zero)
        {
            puntosCamino.Add(destino);
            return;
        }

        puntosCamino.Add(waypointRodeo);
        AñadirRutaConWaypoints(waypointRodeo, destino, profundidad + 1);
    }

    void ChequearRutaPeriodica()
    {
        if (!moviendose || puntosCamino.Count == 0) return;

        tiempoUltimoChequeo += Time.fixedDeltaTime;
        if (tiempoUltimoChequeo < INTERVALO_CHEQUEO_RUTA) return;
        tiempoUltimoChequeo = 0f;

        // Cooldown: no recalcular si lo hicimos hace muy poco
        if (Time.time - tiempoUltimoRecalculo < COOLDOWN_RECALCULO) return;

        Vector3 objetivo = puntosCamino[0];
        Vector2 dir = ((Vector2)objetivo - rb.position).normalized;
        float dist = Vector2.Distance(rb.position, objetivo);

        // Si el siguiente waypoint está muy cerca, no chequeamos (probablemente es un waypoint de rodeo válido)
        if (dist < 0.5f) return;

        Vector2 origenCast = rb.position + col.offset + dir * 0.2f;
        RaycastHit2D hit = Physics2D.CapsuleCast(
            origenCast,
            col.size * 1.1f,
            col.direction, 0f,
            dir, dist,
            capEdificios);

        if (hit.collider != null)
        {
            Vector3 destinoFinal = puntosCamino[puntosCamino.Count - 1];
            CalcularRutaInteligente(transform.position, destinoFinal);
            tiempoUltimoRecalculo = Time.time;
        }
    }

    Vector3 EncontrarWaypointRodeo(Vector3 inicio, Vector3 destino, Collider2D obstaculoDetectado)
    {
        Vector3 centroObstaculo = obstaculoDetectado.bounds.center;
        int numCandidatos = Physics2D.OverlapCircleNonAlloc(centroObstaculo, radioBusquedaWaypoints, _bufferOverlap, capaWaypoints);
        if (numCandidatos == 0)
            numCandidatos = Physics2D.OverlapCircleNonAlloc(inicio, radioBusquedaWaypoints * 2f, _bufferOverlap, capaWaypoints);

        Vector3 mejorWaypoint = Vector3.zero;
        float menorCosto = Mathf.Infinity;

        for (int i = 0; i < numCandidatos; i++)
        {
            Collider2D wp = _bufferOverlap[i];
            Vector3 posWp = wp.transform.position;

            Vector2 dirAlWp = ((Vector2)posWp - (Vector2)inicio).normalized;
            float distAlWp = Vector2.Distance(inicio, posWp);
            RaycastHit2D hitAlWp = Physics2D.CapsuleCast(
                (Vector2)inicio + col.offset,
                col.size * 1.2f,
                col.direction, 0f,
                dirAlWp, distAlWp,
                capEdificios);
            if (hitAlWp.collider != null) continue;

            Vector2 dirWpADestino = ((Vector2)destino - (Vector2)posWp).normalized;
            float distWpADestino = Vector2.Distance(posWp, destino);
            RaycastHit2D hitWpDestino = Physics2D.CapsuleCast(
                (Vector2)posWp + col.offset,
                col.size * 1.2f,
                col.direction, 0f,
                dirWpADestino, distWpADestino,
                capEdificios);
            if (hitWpDestino.collider != null) continue;

            float costo = distAlWp + distWpADestino;
            if (costo < menorCosto)
            {
                menorCosto = costo;
                mejorWaypoint = posWp;
            }
        }

        return mejorWaypoint;
    }

    void Mover()
    {
        if (!moviendose || puntosCamino.Count == 0) return;

        Vector3 objetivoActual = puntosCamino[0];
        Vector2 delta = (Vector2)objetivoActual - rb.position;
        float distSqr = delta.sqrMagnitude;

        if (distSqr < distanciaParada * distanciaParada)
        {
            puntosCamino.RemoveAt(0);
            tiempoAtascado = 0f;
            if (puntosCamino.Count == 0) moviendose = false;
            return;
        }

        Vector2 direccion = delta / Mathf.Sqrt(distSqr);
        Vector2 paso = direccion * (velocidad * Time.fixedDeltaTime);
        Vector2 nuevaPos = rb.position + paso;

        if (Physics2D.OverlapCapsule(nuevaPos + col.offset, col.size, col.direction, 0f, capEdificios) != null)
        {
            // Intentar deslizarse por el eje X
            Vector2 posX = new Vector2(nuevaPos.x, rb.position.y);
            Vector2 posY = new Vector2(rb.position.x, nuevaPos.y);

            if (Mathf.Abs(paso.x) > 0.001f &&
                Physics2D.OverlapCapsule(posX + col.offset, col.size, col.direction, 0f, capEdificios) == null)
            {
                nuevaPos = posX;
                direccionMovimiento = new Vector2(direccion.x, 0f).normalized;
            }
            else if (Mathf.Abs(paso.y) > 0.001f &&
                     Physics2D.OverlapCapsule(posY + col.offset, col.size, col.direction, 0f, capEdificios) == null)
            {
                nuevaPos = posY;
                direccionMovimiento = new Vector2(0f, direccion.y).normalized;
            }
            else
            {
                // Completamente bloqueado, no mover — el stuck detection recalculará
                nuevaPos = rb.position;
                direccionMovimiento = direccion;
            }
        }
        else
        {
            direccionMovimiento = direccion;
        }

        if (nuevaPos != rb.position)
            rb.MovePosition(nuevaPos);

        // Stuck detection siempre activo, independientemente del bloqueo
        float movidaSqr = (rb.position - ultimaPosicion).sqrMagnitude;
        if (movidaSqr < DISTANCIA_MINIMA_MOVIMIENTO * DISTANCIA_MINIMA_MOVIMIENTO)
        {
            tiempoAtascado += Time.fixedDeltaTime;
            if (tiempoAtascado >= TIEMPO_CANCELAR)
            {
                Vector3 destinoFinal = puntosCamino[puntosCamino.Count - 1];
                CalcularRutaInteligente(transform.position, destinoFinal);
                tiempoAtascado = 0f;
                tiempoUltimoRecalculo = Time.time;
            }
        }
        else
            tiempoAtascado = 0f;

        ultimaPosicion = rb.position;
    }

    WaypointPuente EncontrarMejorPuente(Vector3 inicio, Vector3 destino)
    {
        int numWaypoints = Physics2D.OverlapCircleNonAlloc(inicio, radioBusquedaPuente, _bufferOverlap, capaWaypointPuente);
        WaypointPuente mejorPuente = null;
        float menorCosto = Mathf.Infinity;

        for (int i = 0; i < numWaypoints; i++)
        {
            Collider2D c = _bufferOverlap[i];
            WaypointPuente wp = c.GetComponent<WaypointPuente>();
            if (wp == null || wp.waypointConectado == null) continue;

            if (!CaminoTieneAgua(inicio, wp.transform.position) &&
                !CaminoTieneAgua(wp.waypointConectado.transform.position, destino))
            {
                float costo = Vector3.Distance(inicio, wp.transform.position)
                            + Vector3.Distance(wp.waypointConectado.transform.position, destino);
                if (costo < menorCosto)
                {
                    menorCosto = costo;
                    mejorPuente = wp;
                }
            }
        }
        return mejorPuente;
    }

    bool CaminoTieneAgua(Vector3 inicio, Vector3 fin)
    {
        float distancia = Vector3.Distance(inicio, fin);
        if (distancia < 0.1f) return false;

        int muestras = Mathf.CeilToInt(distancia / 2f);
        for (int i = 0; i <= muestras; i++)
        {
            Vector3 punto = Vector3.Lerp(inicio, fin, (float)i / muestras);
            int count = Physics2D.OverlapCircleNonAlloc(punto, 0.2f, _bufferOverlap, capaAgua);
            if (count > 0)
            {
                if (Physics2D.OverlapCircleNonAlloc(punto, 0.2f, _bufferOverlap, capaWaypointPuente) == 0)
                    return true;
            }
        }
        return false;
    }

    bool EsSueloValido(Vector3 posicion)
    {
        if (Physics2D.OverlapCircle(posicion, 0.3f, capEdificios) != null) return false;
        return Physics2D.OverlapCircle(posicion, 0.3f, capaSuelo) != null;
    }

    void ConfigurarCooldown()
    {
        puedeClickar = false;
        ultimoClickTime = Time.time;
    }

    void ActualizarCooldown()
    {
        if (!puedeClickar && Time.time - ultimoClickTime >= cooldownClick)
            puedeClickar = true;
    }

    void ActualizarAnimacion()
    {
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

        Sprite s;
        if (angulo >= 337.5f || angulo < 22.5f)
            s = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo < 67.5f)
            s = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo < 112.5f)
            s = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo < 157.5f)
            s = idle ? atrasIzquierda_Idle : (alternar ? atrasIzquierda_L : atrasIzquierda_R);
        else if (angulo < 202.5f)
            s = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo < 247.5f)
            s = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo < 292.5f)
            s = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else
            s = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);

        spriteRenderer.sprite = s;
    }
}