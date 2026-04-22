using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EdificioClick : MonoBehaviour
{
    public enum TipoEdificio { CuartelSoldados, FabricaTanques }

    [Header("Configuración")]
    public TipoEdificio tipoDeEdificio = TipoEdificio.CuartelSoldados;

    [Header("Paneles UI")]
    public GameObject panelSoldadosUI;
    public GameObject panelTanquesUI;

    private PanelSoldadosUI scriptPanelSoldados;
    private PanelTanquesUI scriptPanelTanques;
    private BuildingProducer myProducer;

    void Start()
    {
        myProducer = GetComponent<BuildingProducer>();
        if (panelSoldadosUI == null) panelSoldadosUI = FindObjectOfType<PanelSoldadosUI>(true)?.gameObject;
        if (panelSoldadosUI != null) scriptPanelSoldados = panelSoldadosUI.GetComponent<PanelSoldadosUI>();
        if (panelTanquesUI == null) panelTanquesUI = FindObjectOfType<PanelTanquesUI>(true)?.gameObject;
        if (panelTanquesUI != null) scriptPanelTanques = panelTanquesUI.GetComponent<PanelTanquesUI>();
    }

    // Llamado por BuildingClickManager
    public void OnClicked()
    {
        SoundColector.Instance?.PlayUiPanelOpen();
        GameEvents.RaiseBuildingSelected();

        if (tipoDeEdificio == TipoEdificio.CuartelSoldados) AbrirSoldados();
        else AbrirTanques();
    }

    void AbrirSoldados()
    {
        if (scriptPanelSoldados == null) return;
        if (panelSoldadosUI.activeSelf && scriptPanelSoldados.GetCurrentProducer() == myProducer)
            scriptPanelSoldados.HidePanel();
        else
        {
            if (scriptPanelTanques != null) scriptPanelTanques.HidePanel();
            scriptPanelSoldados.ConfigurarPanel(this);
        }
    }

    void AbrirTanques()
    {
        if (scriptPanelTanques == null) return;
        if (panelTanquesUI.activeSelf && scriptPanelTanques.GetCurrentProducer() == myProducer)
            scriptPanelTanques.HidePanel();
        else
        {
            if (scriptPanelSoldados != null) scriptPanelSoldados.HidePanel();
            scriptPanelTanques.ConfigurarPanel(this);
        }
    }
}