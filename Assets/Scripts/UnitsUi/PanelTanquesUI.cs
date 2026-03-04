using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelTanquesUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject panelRoot;
    public Button closeButton;
    public Button tanqueButton;
    public TextMeshProUGUI costText;

    [Header("Datos del Tanque")]
    public BuildingData datosDelTanque;

    [Header("Costes")]
    public int costTanque = 200;
    private int pobCosteTanque = 1;

    // Referencia al script productor del edificio seleccionado
    private BuildingProducer currentProducer = null;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
        if (tanqueButton != null) tanqueButton.onClick.AddListener(OnRecruitTanque);

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
        if (panelRoot.activeSelf) UpdateUI();
    }

    void UpdateUI()
    {
        if (costText != null) costText.text = $"{costTanque}$";

        bool isBusy = (currentProducer != null && currentProducer.isBusy);

        if (tanqueButton != null)
        {
            bool tieneDinero = true;
            bool tieneSitio = true;

            if (MoneyManager.Instance != null)
                tieneDinero = MoneyManager.Instance.CurrentMoney >= costTanque;

            if (PopulationManager.Instance != null)
                tieneSitio = PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Tanque, pobCosteTanque);

            // Se desactiva si no hay recursos O si el edificio está ocupado
            tanqueButton.interactable = tieneDinero && tieneSitio && !isBusy;
        }
    }

    public void ConfigurarPanel(EdificioClick edificio)
    {
        currentProducer = edificio.GetComponent<BuildingProducer>();

        if (currentProducer == null)
        {
            Debug.LogError("El edificio Tanques no tiene el script 'BuildingProducer'.");
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateUI();
    }

    public void OnRecruitTanque()
    {
        if (currentProducer == null || currentProducer.isBusy) return;

        // 1. CHEQUEO DE POBLACIÓN
        if (!PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Tanque, pobCosteTanque))
        {
            Debug.Log("¡Necesitas construir más Garajes!");
            return;
        }

        // 2. CHEQUEO DE DINERO
        if (MoneyManager.Instance.CurrentMoney < costTanque)
        {
            Debug.Log("No tienes dinero suficiente.");
            return;
        }

        // 3. PAGAR
        MoneyManager.Instance.SpendMoney(costTanque);

        // 4. ORDENAR AL EDIFICIO QUE CONSTRUYA
        if (datosDelTanque != null)
        {
            currentProducer.StartProduction(datosDelTanque);
        }
        else
        {
            Debug.LogError("Falta asignar el BuildingData del tanque.");
        }
    }

    // Añade esto casi al final de la clase
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