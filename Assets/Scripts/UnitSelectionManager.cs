using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems; // --- NUEVO: NECESARIO PARA DETECTAR UI ---

public class UnitSelectionManager : MonoBehaviour
{
    [Header("Configuración de Selección")]
    public LayerMask capaUnidades;
    public Sprite spriteCuadroSeleccion;

    [Header("Configuración de Formación")]
    public float radioFormacion = 1.5f;
    public bool usarFormacionCircular = true;

    [Header("Tweaks de Audio")]
    [Tooltip("Tiempo mínimo entre sonidos de movimiento.")]
    public float intervaloMinSomMovimiento = 0.15f;
    private float ultimoSomMovimientoTime = -999f;

    [Header("Click Direito (Mover/Atacar)")]
    public LayerMask capaAlvosAtaque;

    // Variables internas
    private Vector3 inicioArrastre;
    private Vector3 finArrastre;
    private bool arrastrando = false;
    private Camera cam;

    private List<ISelectableUnit> unidadesSeleccionadas = new List<ISelectableUnit>();
    private ISelectableUnit unidadVozPrincipal = null;

    private bool hasGroupVoiceGender = false;
    private SoundColector.VoiceGender cachedGroupVoiceGender = SoundColector.VoiceGender.Male;
    private int cachedSelectionSignature = 0;

    // Easter Eggs
    private const int EasterEggFirstClick = 5;
    private const int EasterEggTotal = 4;
    private ISelectableUnit easterEggTargetUnit = null;
    private int easterEggClickCount = 0;

    private ISelectableUnit unidadClicadaMouseDown = null;
    private bool shiftMouseDown = false;

    private SpriteRenderer spriteRenderer;
    private GameObject cuadroObj;

    void Start()
    {
        cam = Camera.main;

        cuadroObj = new GameObject("CuadroSeleccion");
        spriteRenderer = cuadroObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteCuadroSeleccion;
        spriteRenderer.color = new Color(0, 0.5f, 1f, 0.3f);
        spriteRenderer.sortingOrder = 100;
        cuadroObj.SetActive(false);
    }

    void Update()
    {
        ProcesarSeleccion();
        ProcesarMovimiento();
    }

    // ---------------------------------------------------------
    // CLICK IZQUIERDO (Seleccionar)
    // ---------------------------------------------------------
    void ProcesarSeleccion()
    {
        // 1. Click Abajo
        if (Input.GetMouseButtonDown(0))
        {
            // --- NUEVO BLOQUE: SI TOCAMOS UI (MINIMAP), SALIMOS ---
            // Esto evita que empiece el arrastre o deseleccione unidades
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            // ------------------------------------------------------

            inicioArrastre = Input.mousePosition;
            finArrastre = inicioArrastre;
            arrastrando = true;

            unidadClicadaMouseDown = null;
            shiftMouseDown = Input.GetKey(KeyCode.LeftShift);

            RaycastHit2D hit = Physics2D.Raycast(
                cam.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero,
                Mathf.Infinity,
                capaUnidades
            );

            if (hit.collider != null)
            {
                ISelectableUnit unidad = hit.collider.GetComponent<ISelectableUnit>();

                if (unidad != null)
                {
                    unidadClicadaMouseDown = unidad;
                    unidadVozPrincipal = unidad;

                    bool shift = shiftMouseDown;
                    bool clickedSameAsSoleSelected = unidadesSeleccionadas.Count == 1 && ReferenceEquals(unidadesSeleccionadas[0], unidad);

                    if (!shift)
                    {
                        if (!clickedSameAsSoleSelected)
                            DeseleccionarTodas(resetEasterEggs: true);

                        DeseleccionarTodas(resetEasterEggs: !clickedSameAsSoleSelected);
                    }
                    else
                    {
                        ResetEasterEggState();
                    }

                    bool eraNueva = !unidadesSeleccionadas.Contains(unidad);
                    if (eraNueva) SeleccionarUnidad(unidad);

                    if (eraNueva) PlaySelectionSoundFor(unidad);
                }
            }
            else
            {
                // Click en el vacío: Deseleccionar todo
                if (!shiftMouseDown)
                {
                    DeseleccionarTodas(resetEasterEggs: true);
                }
            }
        }

        // 2. Arrastrando
        if (arrastrando && Input.GetMouseButton(0))
        {
            finArrastre = Input.mousePosition;
            DibujarCuadroSeleccion();
        }

        // 3. Soltar Click
        if (Input.GetMouseButtonUp(0))
        {
            // Solo procesamos si estabamos arrastrando de verdad
            if (arrastrando)
            {
                bool fueSeleccionArea = Vector3.Distance(inicioArrastre, finArrastre) > 10f;

                if (fueSeleccionArea)
                {
                    SeleccionarUnidadesEnCuadro();
                }
                else
                {
                    if (unidadClicadaMouseDown != null)
                    {
                        RegisterEasterEggClick(unidadClicadaMouseDown, shiftMouseDown);
                    }
                }

                unidadClicadaMouseDown = null;
                shiftMouseDown = false;
                arrastrando = false; // Resetear bandera
                OcultarCuadroSeleccion();
            }
        }
    }

    // ---------------------------------------------------------
    // CLICK DERECHO (Mover)
    // ---------------------------------------------------------
    void ProcesarMovimiento()
    {
        // También evitamos movernos si hacemos click derecho en la UI (opcional)
        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        }

        // Limpiamos la lista por si alguna unidad murió
        unidadesSeleccionadas.RemoveAll(u => u == null || u.gameObject == null);

        if (unidadesSeleccionadas.Count == 0)
        {
            ResetEasterEggState();
        }

        if (Input.GetMouseButtonDown(1) && unidadesSeleccionadas.Count > 0)
        {

            Vector3 destino = cam.ScreenToWorldPoint(Input.mousePosition);
            destino.z = 0;

            // Calculamos formaciones
            Vector3[] destinos = CalcularDestinosDistribuidos(destino, unidadesSeleccionadas.Count);

            for (int i = 0; i < unidadesSeleccionadas.Count; i++)
            {
                if (unidadesSeleccionadas[i] != null)
                {
                    unidadesSeleccionadas[i].MoverADestino(destinos[i]);
                }
            }

            //Audio de movimiento
            if (SoundColector.Instance != null &&
                Time.time - ultimoSomMovimientoTime >= intervaloMinSomMovimiento)
            {
                int tankCount = unidadesSeleccionadas.Count(u => IsTank(u));
                int infantryCount = Mathf.Max(0, unidadesSeleccionadas.Count - tankCount);

                var vozUnit = (unidadVozPrincipal != null) ? unidadVozPrincipal : unidadesSeleccionadas[0];
                int id = (vozUnit != null && vozUnit.gameObject != null) ? vozUnit.gameObject.GetInstanceID() : 0;

                SoundColector.Instance.SetVoiceContextUnit(id);
                var gender = SoundColector.Instance.GetOrAssignLockedGenderForUnit(id);

                SoundColector.Instance.PlayUnitMoveVoice(infantryCount, tankCount, gender);

                ultimoSomMovimientoTime = Time.time;
            }
        }
    }

    // ---------------------------------------------------------
    // GESTIÓN DE LA LISTA DE SELECCIÓN
    // ---------------------------------------------------------

    void SeleccionarUnidad(ISelectableUnit unidad)
    {
        if (!unidadesSeleccionadas.Contains(unidad))
        {
            unidadesSeleccionadas.Add(unidad);
            unidad.Seleccionar();
        }
    }

    void DeseleccionarTodas(bool resetEasterEggs = true)
    {
        foreach (ISelectableUnit unidad in unidadesSeleccionadas)
        {
            if (unidad != null && unidad.gameObject != null)
            {
                unidad.Deseleccionar();
            }
        }
        unidadesSeleccionadas.Clear();

        if (UnitHUDManager.Instance != null)
        {
            UnitHUDManager.Instance.SeleccionarUnidad(null);
        }

        if (resetEasterEggs)
        {
            ResetEasterEggState();
        }

        hasGroupVoiceGender = false;
        cachedSelectionSignature = 0;
        unidadVozPrincipal = null;

        if (SoundColector.Instance != null)
            SoundColector.Instance.ClearVoiceContextUnit();
    }

    void ResetEasterEggState()
    {
        easterEggTargetUnit = null;
        easterEggClickCount = 0;
    }

    void RegisterEasterEggClick(ISelectableUnit clickedUnit, bool shiftHeld)
    {
        if (shiftHeld)
        {
            ResetEasterEggState();
            return;
        }

        if (clickedUnit == null || clickedUnit.gameObject == null)
        {
            ResetEasterEggState();
            return;
        }

        if (unidadesSeleccionadas.Count != 1 || !ReferenceEquals(unidadesSeleccionadas[0], clickedUnit))
        {
            ResetEasterEggState();
            return;
        }

        if (!ReferenceEquals(easterEggTargetUnit, clickedUnit))
        {
            easterEggTargetUnit = clickedUnit;
            easterEggClickCount = 0;
        }

        easterEggClickCount++;

        int eggIndex = easterEggClickCount - (EasterEggFirstClick - 1);
        if (eggIndex >= 1 && eggIndex <= EasterEggTotal)
        {
            int tankCount = unidadesSeleccionadas.Count(u => IsTank(u));
            int infantryCount = Mathf.Max(0, unidadesSeleccionadas.Count - tankCount);

            if (SoundColector.Instance != null)
            {
                int id = clickedUnit.gameObject.GetInstanceID();
                SoundColector.Instance.SetVoiceContextUnit(id);
                SoundColector.Instance.GetOrAssignLockedGenderForUnit(id);
            }

            GameEvents.RaiseUnitEasterEgg(eggIndex, infantryCount, tankCount);

            if (eggIndex == EasterEggTotal)
            {
                easterEggClickCount = EasterEggFirstClick - 1;
            }
        }
    }

    void SeleccionarUnidadesEnCuadro()
    {
        ResetEasterEggState();

        Vector2 min = cam.ScreenToWorldPoint(new Vector3(
            Mathf.Min(inicioArrastre.x, finArrastre.x),
            Mathf.Min(inicioArrastre.y, finArrastre.y), 0));

        Vector2 max = cam.ScreenToWorldPoint(new Vector3(
            Mathf.Max(inicioArrastre.x, finArrastre.x),
            Mathf.Max(inicioArrastre.y, finArrastre.y), 0));

        Collider2D[] unidadesEnArea = Physics2D.OverlapAreaAll(min, max, capaUnidades);

        if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeseleccionarTodas(resetEasterEggs: true);
        }

        ISelectableUnit primeraNueva = null;

        foreach (Collider2D collider in unidadesEnArea)
        {
            ISelectableUnit unidad = collider.GetComponent<ISelectableUnit>();
            if (unidad != null)
            {
                bool eraNueva = !unidadesSeleccionadas.Contains(unidad);
                SeleccionarUnidad(unidad);

                if (eraNueva && primeraNueva == null) primeraNueva = unidad;
            }
        }

        if (primeraNueva != null)
        {
            unidadVozPrincipal = primeraNueva;
            PlaySelectionSoundFor(primeraNueva);
        }

    }

    // ---------------------------------------------------------
    // FORMACIONES
    // ---------------------------------------------------------
    Vector3[] CalcularDestinosDistribuidos(Vector3 destinoCentral, int cantidadUnidades)
    {
        Vector3[] destinos = new Vector3[cantidadUnidades];

        if (cantidadUnidades == 1)
        {
            destinos[0] = destinoCentral;
            return destinos;
        }

        if (usarFormacionCircular)
        {
            for (int i = 0; i < cantidadUnidades; i++)
            {
                float angulo = i * (2f * Mathf.PI / cantidadUnidades);
                float x = Mathf.Cos(angulo) * radioFormacion;
                float y = Mathf.Sin(angulo) * radioFormacion;
                destinos[i] = destinoCentral + new Vector3(x, y, 0);
            }
        }
        else
        {
            int filas = Mathf.CeilToInt(Mathf.Sqrt(cantidadUnidades));
            int columnas = Mathf.CeilToInt((float)cantidadUnidades / filas);

            int index = 0;
            for (int fila = 0; fila < filas; fila++)
            {
                for (int columna = 0; columna < columnas; columna++)
                {
                    if (index >= cantidadUnidades) break;

                    float x = (columna - (columnas - 1) * 0.5f) * radioFormacion;
                    float y = (fila - (filas - 1) * 0.5f) * radioFormacion;
                    destinos[index] = destinoCentral + new Vector3(x, y, 0);
                    index++;
                }
            }
        }

        return destinos;
    }

    // ---------------------------------------------------------
    // VISUALES Y SONIDO
    // ---------------------------------------------------------
    void DibujarCuadroSeleccion()
    {
        Vector3 inicioMundo = cam.ScreenToWorldPoint(new Vector3(inicioArrastre.x, inicioArrastre.y, 0));
        Vector3 finMundo = cam.ScreenToWorldPoint(new Vector3(finArrastre.x, finArrastre.y, 0));
        inicioMundo.z = 0; finMundo.z = 0;

        Vector3 centro = (inicioMundo + finMundo) / 2f;
        Vector3 tamaño = new Vector3(Mathf.Abs(finMundo.x - inicioMundo.x), Mathf.Abs(finMundo.y - inicioMundo.y), 1f);

        cuadroObj.transform.position = centro;
        cuadroObj.transform.localScale = tamaño;
        cuadroObj.SetActive(true);
    }

    void OcultarCuadroSeleccion()
    {
        cuadroObj.SetActive(false);
    }

    bool IsTank(ISelectableUnit u)
    {
        if (u == null || u.gameObject == null) return false;
        return u.gameObject.GetComponentInParent<TankShooting>() != null;
    }

    int ComputeSelectionSignature()
    {
        int count = (unidadesSeleccionadas != null) ? unidadesSeleccionadas.Count : 0;
        int xor = 0;
        int sum = 0;

        for (int i = 0; i < count; i++)
        {
            var u = unidadesSeleccionadas[i];
            if (u == null || u.gameObject == null) continue;

            int id = u.gameObject.GetInstanceID();
            xor ^= id;
            sum += id;
        }

        unchecked
        {
            return (count * 73856093) ^ (xor * 19349663) ^ (sum * 83492791);
        }
    }

    void RefreshGroupVoiceGenderIfSelectionChanged()
    {
        if (SoundColector.Instance == null || unidadesSeleccionadas == null || unidadesSeleccionadas.Count == 0)
        {
            hasGroupVoiceGender = false;
            cachedSelectionSignature = 0;
            return;
        }

        int sig = ComputeSelectionSignature();
        if (hasGroupVoiceGender && sig == cachedSelectionSignature)
            return;

        int male = 0;
        int female = 0;

        for (int i = 0; i < unidadesSeleccionadas.Count; i++)
        {
            var u = unidadesSeleccionadas[i];
            if (u == null || u.gameObject == null) continue;

            int id = u.gameObject.GetInstanceID();
            var g = SoundColector.Instance.GetOrAssignLockedGenderForUnit(id);

            if (g == SoundColector.VoiceGender.Female) female++;
            else male++;
        }

        if (female > male) cachedGroupVoiceGender = SoundColector.VoiceGender.Female;
        else if (male > female) cachedGroupVoiceGender = SoundColector.VoiceGender.Male;
        else
        {
            float pFemale = SoundColector.Instance.unitFemaleChance;
            cachedGroupVoiceGender = (UnityEngine.Random.value < pFemale)
                ? SoundColector.VoiceGender.Female
                : SoundColector.VoiceGender.Male;
        }

        hasGroupVoiceGender = true;
        cachedSelectionSignature = sig;
    }


    void PlaySelectionSoundFor(ISelectableUnit unidad)
    {
        if (SoundColector.Instance == null) return;

        int tankCount = unidadesSeleccionadas.Count(u => IsTank(u));
        int infantryCount = Mathf.Max(0, unidadesSeleccionadas.Count - tankCount);

        RefreshGroupVoiceGenderIfSelectionChanged();
        SoundColector.Instance.PlayUnitSelectionVoice(infantryCount, tankCount, cachedGroupVoiceGender);
    }
}