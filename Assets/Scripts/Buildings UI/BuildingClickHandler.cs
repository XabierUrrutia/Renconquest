using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
// [RequireComponent(typeof(BuildingProducer))] // Descomenta esto si quieres forzar que siempre tenga el producer
public class EdificioClick : MonoBehaviour
{
    public enum TipoEdificio
    {
        CuartelSoldados,
        FabricaTanques
    }

    [Header("Configuración")]
    public TipoEdificio tipoDeEdificio = TipoEdificio.CuartelSoldados;

    [Header("Paneles UI")]
    public GameObject panelSoldadosUI;
    public GameObject panelTanquesUI;

    // Referencias internas
    private PanelSoldadosUI scriptPanelSoldados;
    private PanelTanquesUI scriptPanelTanques;

    // Referencia al productor de ESTE edificio
    private BuildingProducer myProducer;
    private bool clickWasOnThisBuilding = false;

    void Start()
    {
        // Obtener mi propio componente de producción
        myProducer = GetComponent<BuildingProducer>();

        // Buscar referencias automáticamente si están vacías
        if (panelSoldadosUI == null) panelSoldadosUI = FindObjectOfType<PanelSoldadosUI>(true)?.gameObject;
        if (panelSoldadosUI != null) scriptPanelSoldados = panelSoldadosUI.GetComponent<PanelSoldadosUI>();

        if (panelTanquesUI == null) panelTanquesUI = FindObjectOfType<PanelTanquesUI>(true)?.gameObject;
        if (panelTanquesUI != null) scriptPanelTanques = panelTanquesUI.GetComponent<PanelTanquesUI>();
    }

    private void OnMouseDown()
    {
        // Evitar clic si tocamos la UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        clickWasOnThisBuilding = true;
        // GameEvents.RaiseBuildingSelected(); // Descomenta si usas tu sistema de eventos

        TogglePanel();
        // SoundColector.Instance?.PlayUiPanelOpen(); // Descomenta si tienes sonido

        clickWasOnThisBuilding = false;
    }

    public void TogglePanel()
    {
        if (!clickWasOnThisBuilding) return;

        if (tipoDeEdificio == TipoEdificio.CuartelSoldados)
        {
            AbrirSoldados();
        }
        else if (tipoDeEdificio == TipoEdificio.FabricaTanques)
        {
            AbrirTanques();
        }
    }

    void AbrirSoldados()
    {
        if (scriptPanelSoldados == null) return;

        // LÓGICA CORREGIDA:
        // Comparamos si el panel está activo Y si el "Producer" que está mirando el panel es EL MÍO.
        if (panelSoldadosUI.activeSelf && scriptPanelSoldados.GetCurrentProducer() == myProducer)
        {
            scriptPanelSoldados.HidePanel();
        }
        else
        {
            // Cerramos el otro panel si está abierto
            if (scriptPanelTanques != null) scriptPanelTanques.HidePanel();

            // IMPORTANTE: NO PAUSAR EL TIEMPO (Time.timeScale = 0) 
            // PORQUE SI NO EL SLIDER DE CONSTRUCCIÓN NO AVANZA.

            scriptPanelSoldados.ConfigurarPanel(this);
        }
    }

    void AbrirTanques()
    {
        if (scriptPanelTanques == null) return;

        // LÓGICA CORREGIDA:
        if (panelTanquesUI.activeSelf && scriptPanelTanques.GetCurrentProducer() == myProducer)
        {
            scriptPanelTanques.HidePanel();
        }
        else
        {
            if (scriptPanelSoldados != null) scriptPanelSoldados.HidePanel();

            // IMPORTANTE: NO PAUSAR EL TIEMPO

            scriptPanelTanques.ConfigurarPanel(this);
        }
    }
}