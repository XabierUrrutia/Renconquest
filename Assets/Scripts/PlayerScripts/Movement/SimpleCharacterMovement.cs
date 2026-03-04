using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SelectableUnit))] // Obliga a tener el componente visual
public class SimpleCharacterMovement : MonoBehaviour, ISelectableUnit
{
    [Header("Movimiento Básico")]
    public float velocidad = 4f;
    public float distanciaParada = 0.1f;

    [Header("Cooldown")]
    public float cooldownClick = 1.5f;
    private float ultimoClickTime;
    private bool puedeClickar = true;

    [Header("Detección de Terreno")]
    public LayerMask capaSuelo;
    public LayerMask capaAgua;
    public LayerMask capaWaypointPuente;

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

    // YA NO USAMOS ESTO MANUALMENTE (Lo gestiona SelectableUnit)
    // public GameObject indicadorSeleccion; 

    // Referencia al componente unificador
    private SelectableUnit selectableUnitComponent;

    // Física
    private Rigidbody2D rb;
    private CapsuleCollider2D col;

    // Variables internas
    private Vector3 objetivo;
    private bool moviendose = false;
    private List<Vector3> puntosCamino = new List<Vector3>();
    private Camera cam;
    private Vector2 direccionMovimiento;
    private SpriteRenderer spriteRenderer;

    // Animación
    private float temporizadorAnim = 0f;
    private bool alternarAnim = false;
    private Vector2 ultimaDireccion = new Vector2(1, -1);

    // Estado Selección
    private bool estaSeleccionado = false;

    // Anti-atasco
    private Vector2 ultimaPosicion;
    private float tiempoAtascado = 0f;
    private const float TIEMPO_LIMITE_ATASCO = 0.8f;
    private const float DISTANCIA_MINIMA_MOVIMIENTO = 0.02f;

    void Start()
    {
        cam = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        objetivo = transform.position;

        // Obtenemos el componente que controla la flecha y el HUD
        selectableUnitComponent = GetComponent<SelectableUnit>();

        ActualizarSprite(ultimaDireccion, false, true);
    }

    void Update()
    {
        ActualizarCooldown();
        ActualizarAnimacion();
    }

    void FixedUpdate()
    {
        Mover();
    }

    // --- INTERFAZ ISelectableUnit (La clave para que funcione el Manager) ---

    public void Seleccionar()
    {
        estaSeleccionado = true;
        // Delegamos lo visual al componente SelectableUnit
        if (selectableUnitComponent != null)
            selectableUnitComponent.ShowSelection(true);

        Debug.Log($"{name} seleccionado");
    }

    public void Deseleccionar()
    {
        estaSeleccionado = false;
        // Delegamos lo visual al componente SelectableUnit
        if (selectableUnitComponent != null)
            selectableUnitComponent.ShowSelection(false);

        Debug.Log($"{name} deseleccionado");
    }

    // --- MOVIMIENTO ---

    public void MoverADestino(Vector3 destino)
    {
        if (puedeClickar && estaSeleccionado)
        {
            Vector3 posicionRaton = destino;
            posicionRaton.z = 0;

            if (EsSueloValido(posicionRaton))
            {
                ConfigurarCooldown();
                CalcularRutaInteligente(transform.position, posicionRaton);

                if (prefabMarcadorClick != null)
                {
                    GameObject marcador = Instantiate(prefabMarcadorClick, posicionRaton, Quaternion.identity);
                    Destroy(marcador, 1f);
                }
            }
        }
    }

    // (El resto de tu lógica de movimiento se mantiene igual)
    void CalcularRutaInteligente(Vector3 inicio, Vector3 destino)
    {
        puntosCamino.Clear();
        moviendose = false;

        if (!CaminoTieneAgua(inicio, destino))
        {
            puntosCamino.Add(destino);
            moviendose = true;
        }
        else
        {
            WaypointPuente puenteUtil = EncontrarPuenteSimple(inicio, destino);

            if (puenteUtil != null && puenteUtil.waypointConectado != null)
            {
                if (!CaminoTieneAgua(puenteUtil.waypointConectado.transform.position, destino))
                {
                    puntosCamino.Add(puenteUtil.transform.position);
                    puntosCamino.Add(puenteUtil.waypointConectado.transform.position);
                    puntosCamino.Add(destino);
                    moviendose = true;
                }
            }
        }
    }

    WaypointPuente EncontrarPuenteSimple(Vector3 inicio, Vector3 destino)
    {
        Collider2D[] todosWaypoints = Physics2D.OverlapCircleAll(inicio, 12f, capaWaypointPuente);
        WaypointPuente mejorPuente = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Collider2D collider in todosWaypoints)
        {
            WaypointPuente waypoint = collider.GetComponent<WaypointPuente>();
            if (waypoint != null && waypoint.waypointConectado != null)
            {
                bool caminoAlPuenteSeguro = !CaminoTieneAgua(inicio, waypoint.transform.position);
                bool caminoDelPuenteSeguro = !CaminoTieneAgua(waypoint.waypointConectado.transform.position, destino);

                if (caminoAlPuenteSeguro && caminoDelPuenteSeguro)
                {
                    float distancia = Vector3.Distance(inicio, waypoint.transform.position);
                    if (distancia < menorDistancia)
                    {
                        menorDistancia = distancia;
                        mejorPuente = waypoint;
                    }
                }
            }
        }
        return mejorPuente;
    }

    void Mover()
    {
        if (!moviendose || puntosCamino.Count == 0) return;

        Vector3 objetivoActual = puntosCamino[0];
        Vector3 direccion = (objetivoActual - transform.position).normalized;
        float paso = velocidad * Time.fixedDeltaTime;
        int capEdificios = LayerMask.GetMask("Buildings");

        Vector2 nuevaPos = rb.position + (Vector2)direccion * paso;
        Collider2D choque = Physics2D.OverlapCapsule(nuevaPos + col.offset, col.size * 0.85f, col.direction, 0f, capEdificios);

        if (choque == null)
        {
            rb.MovePosition(nuevaPos);
        }
        else
        {
            // Deslizar por X o por Y
            Vector2 posX = rb.position + new Vector2(direccion.x, 0) * paso;
            Vector2 posY = rb.position + new Vector2(0, direccion.y) * paso;
            bool libreX = Physics2D.OverlapCapsule(posX + col.offset, col.size * 0.85f, col.direction, 0f, capEdificios) == null;
            bool libreY = Physics2D.OverlapCapsule(posY + col.offset, col.size * 0.85f, col.direction, 0f, capEdificios) == null;

            if (libreX && Mathf.Abs(direccion.x) > 0.01f)
                rb.MovePosition(posX);
            else if (libreY && Mathf.Abs(direccion.y) > 0.01f)
                rb.MovePosition(posY);
        }

        direccionMovimiento = direccion;

        // Anti-atasco: si lleva tiempo sin moverse, calcular rodeo por esquina
        float distanciaMovida = Vector2.Distance(rb.position, ultimaPosicion);
        if (distanciaMovida < DISTANCIA_MINIMA_MOVIMIENTO)
        {
            tiempoAtascado += Time.fixedDeltaTime;
            if (tiempoAtascado >= TIEMPO_LIMITE_ATASCO)
            {
                RedirigirAlrededorEdificio(choque, objetivoActual, capEdificios);
                tiempoAtascado = 0f;
            }
        }
        else
        {
            tiempoAtascado = 0f;
        }
        ultimaPosicion = rb.position;

        if (Vector3.Distance(transform.position, objetivoActual) < distanciaParada)
        {
            puntosCamino.RemoveAt(0);
            if (puntosCamino.Count == 0)
                moviendose = false;
        }
    }

    void RedirigirAlrededorEdificio(Collider2D edificio, Vector3 destino, int capEdificios)
    {
        if (edificio == null) return;

        Bounds b = edificio.bounds;
        float margen = Mathf.Max(col.size.x * 1.5f, 0.8f);

        Vector2[] esquinas = new Vector2[]
        {
            new Vector2(b.min.x - margen, b.min.y - margen),
            new Vector2(b.max.x + margen, b.min.y - margen),
            new Vector2(b.min.x - margen, b.max.y + margen),
            new Vector2(b.max.x + margen, b.max.y + margen),
        };

        Vector2 mejorEsquina = Vector2.zero;
        float menorCosto = Mathf.Infinity;

        foreach (Vector2 esquina in esquinas)
        {
            // Esquina libre
            bool libre = Physics2D.OverlapCapsule(esquina + col.offset, col.size * 0.85f, col.direction, 0f, capEdificios) == null;
            if (!libre) continue;

            // Camino hasta la esquina libre
            Vector2 dirEsquina = (esquina - rb.position).normalized;
            float distEsquina = Vector2.Distance(rb.position, esquina);
            RaycastHit2D hitCamino = Physics2D.CapsuleCast(rb.position + col.offset, col.size * 0.85f, col.direction, 0f, dirEsquina, distEsquina, capEdificios);
            if (hitCamino.collider != null) continue;

            float costo = distEsquina + Vector2.Distance(esquina, destino);
            if (costo < menorCosto)
            {
                menorCosto = costo;
                mejorEsquina = esquina;
            }
        }

        if (mejorEsquina != Vector2.zero)
            puntosCamino.Insert(0, new Vector3(mejorEsquina.x, mejorEsquina.y, 0));
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

        Sprite spriteSeleccionado = frenteDerecha_Idle;

        if (angulo >= 337.5f || angulo < 22.5f)
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 22.5f && angulo < 67.5f)
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 67.5f && angulo < 112.5f)
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 112.5f && angulo < 157.5f)
            spriteSeleccionado = idle ? atrasIzquierda_Idle : (alternar ? atrasIzquierda_L : atrasIzquierda_R);
        else if (angulo >= 157.5f && angulo < 202.5f)
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 202.5f && angulo < 247.5f)
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 247.5f && angulo < 292.5f)
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 292.5f && angulo < 337.5f)
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
        int capEdificios = LayerMask.GetMask("Buildings");
        bool hayEdificio = Physics2D.OverlapCircle(posicion, 0.3f, capEdificios) != null;
        if (hayEdificio) return false;
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
        {
            puedeClickar = true;
        }
    }
}