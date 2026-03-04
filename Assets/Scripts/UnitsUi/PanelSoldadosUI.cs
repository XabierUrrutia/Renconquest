using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelSoldadosUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject panelRoot;
    public Button closeButton;
    public Button soldadoButton;
    public Button generalButton;

    [Header("Textos de Coste")]
    public TextMeshProUGUI soldadoCostText;
    public TextMeshProUGUI generalCostText;

    [Header("DATOS DE UNIDADES")]
    public BuildingData datosDelSoldado;
    public BuildingData datosDelGeneral;

    [Header("Configuración Económica")]
    public int costSoldado = 50;
    public int costGeneral = 200;

    [Header("Configuración de Población")]
    private int pobCosteSoldado = 1;
    private int pobCosteGeneral = 2;

    // Referencia al script productor del edificio que tenemos seleccionado actualmente
    private BuildingProducer currentProducer = null;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(HidePanel);

        if (soldadoButton != null) soldadoButton.onClick.AddListener(OnRecruitSoldado);
        if (generalButton != null) generalButton.onClick.AddListener(OnRecruitGeneral);

        UpdateUI();
    }

    void OnEnable()
    {
        if (PopulationManager.Instance != null)
            PopulationManager.Instance.OnPopulationChanged += UpdateUI;
        UpdateUI();
    }

    void OnDisable()
    {
        if (PopulationManager.Instance != null)
            PopulationManager.Instance.OnPopulationChanged -= UpdateUI;
    }

    void Update()
    {
        // Actualizamos cada frame por si el edificio termina de construir, para reactivar botones
        if (panelRoot.activeSelf) UpdateUI();
    }

    void UpdateUI()
    {
        if (soldadoCostText != null) soldadoCostText.text = $"Soldado: {costSoldado}$";
        if (generalCostText != null) generalCostText.text = $"General: {costGeneral}$";

        bool hasMoney = MoneyManager.Instance != null;
        bool hasPop = PopulationManager.Instance != null;
        int currentMoney = hasMoney ? MoneyManager.Instance.CurrentMoney : 0;

        // Comprobamos si el edificio actual está ocupado
        bool isBusy = (currentProducer != null && currentProducer.isBusy);

        // --- LÓGICA BOTÓN SOLDADO ---
        if (soldadoButton != null)
        {
            bool puedePagar = currentMoney >= costSoldado;
            bool tieneSitio = hasPop && PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteSoldado);

            // Solo activamos si tiene dinero, sitio Y el edificio NO está trabajando
            soldadoButton.interactable = puedePagar && tieneSitio && !isBusy;
        }

        // --- LÓGICA BOTÓN GENERAL ---
        if (generalButton != null)
        {
            bool puedePagar = currentMoney >= costGeneral;
            bool tieneSitio = hasPop && PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteGeneral);

            generalButton.interactable = puedePagar && tieneSitio && !isBusy;
        }
    }

    public void ConfigurarPanel(EdificioClick edificio)
    {
        // Intentamos obtener el script BuildingProducer del edificio clickado
        currentProducer = edificio.GetComponent<BuildingProducer>();

        if (currentProducer == null)
        {
            Debug.LogError("El edificio seleccionado no tiene el script 'BuildingProducer'. ¡Añádeselo al prefab!");
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateUI();
    }

    public void OnRecruitSoldado()
    {
        if (currentProducer == null || currentProducer.isBusy) return;

        // 1. Check Población
        if (!PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteSoldado)) return;

        // 2. Check y Pago Dinero
        if (MoneyManager.Instance.CurrentMoney < costSoldado) return;
        MoneyManager.Instance.SpendMoney(costSoldado);

        // 3. Ordenar al edificio que construya (Lógica del Slider y Spawn)
        if (datosDelSoldado != null)
        {
            currentProducer.StartProduction(datosDelSoldado);
        }
    }

    public void OnRecruitGeneral()
    {
        if (currentProducer == null || currentProducer.isBusy) return;

        // 1. Check Población
        if (!PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteGeneral)) return;

        // 2. Check y Pago Dinero
        if (MoneyManager.Instance.CurrentMoney < costGeneral) return;
        MoneyManager.Instance.SpendMoney(costGeneral);

        // 3. Ordenar al edificio
        if (datosDelGeneral != null)
        {
            currentProducer.StartProduction(datosDelGeneral);
        }
    }

    public BuildingProducer GetCurrentProducer()
    {
        return currentProducer;
    }

    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentProducer = null;
    }
}