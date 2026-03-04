using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public enum BuildingType
{
    Pequeno,
    Mediano,
    Grande
}

public class Building : MonoBehaviour
{
    [Header("Configuración del Edificio")]
    public BuildingType buildingType;
    public float conquestTime = 20f;
    public float conquestRange = 3f;

    [Header("Velocidades de Conquista")]
    public float growthSpeed = 1f;      // Velocidad de crecimiento con jugadores
    public float decaySpeed = 0.5f;     // Velocidad de decrecimiento sin jugadores

    [Header("Generación de Dinero")]
    public int moneyPerInterval;
    public float moneyInterval = 5f;

    [Header("Slider de Progreso")]
    public Slider conquestSlider;
    public Vector3 sliderOffset = new Vector3(0, -0f, 0);

    [Header("Estados")]
    public bool isConquered = false;

    [Header("Referencias")]
    public SpriteRenderer spriteRenderer;

    // Colores para diferentes estados
    private Color neutralColor = Color.gray;
    private Color conqueringColor = Color.yellow;
    private Color conqueredColor = Color.green;

    // Variables internas
    private float conquestProgress = 0f;
    private Coroutine conquestCoroutine;
    private Coroutine moneyGenerationCoroutine;

    // Lista de jugadores conquistando este edificio
    private List<PlayerBuildingDetector> conqueringPlayers = new List<PlayerBuildingDetector>();

    void Start()
    {
        InitializeBuilding();
        SetMoneyValuesByType();
        AutoSetupSlider();

        // Iniciar la corrutina de actualización continua
        StartCoroutine(ConquestUpdate());
    }

    void InitializeBuilding()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.color = neutralColor;
    }

    void SetMoneyValuesByType()
    {
        switch (buildingType)
        {
            case BuildingType.Pequeno:
                moneyPerInterval = 10;
                moneyInterval = 5f;
                break;
            case BuildingType.Mediano:
                moneyPerInterval = 25;
                moneyInterval = 5f;
                break;
            case BuildingType.Grande:
                moneyPerInterval = 50;
                moneyInterval = 5f;
                break;
        }
    }

    void AutoSetupSlider()
    {
        if (conquestSlider == null)
        {
            conquestSlider = GetComponentInChildren<Slider>();
        }

        if (conquestSlider != null)
        {
            conquestSlider.minValue = 0f;
            conquestSlider.maxValue = 1f;
            conquestSlider.value = 0f;
            conquestSlider.gameObject.SetActive(false);
            SetupSliderCanvas();
        }
    }

    void SetupSliderCanvas()
    {
        Canvas canvas = conquestSlider.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 1;
        }
    }

    void Update()
    {
        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
        {
            UpdateSliderPosition();
            if (Camera.main != null)
            {
                conquestSlider.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    void UpdateSliderPosition()
    {
        if (conquestSlider != null)
        {
            Canvas canvas = conquestSlider.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.transform.position = transform.position + sliderOffset;
            }
        }
    }

    // Corrutina que actualiza el progreso de conquista continuamente
    private IEnumerator ConquestUpdate()
    {
        while (!isConquered)
        {
            if (conqueringPlayers.Count > 0)
            {
                // Hay jugadores: incrementar progreso
                float progressIncrement = (growthSpeed * conqueringPlayers.Count) / conquestTime;
                conquestProgress += progressIncrement * Time.deltaTime;

                // Asegurar que no exceda el máximo
                conquestProgress = Mathf.Min(conquestProgress, conquestTime);

                // Mostrar slider si hay progreso
                if (conquestProgress > 0 && conquestSlider != null && !conquestSlider.gameObject.activeInHierarchy)
                {
                    conquestSlider.gameObject.SetActive(true);
                    UpdateSliderPosition();
                }
            }
            else if (conquestProgress > 0)
            {
                // No hay jugadores: decrementar progreso
                float progressDecrement = decaySpeed / conquestTime;
                conquestProgress -= progressDecrement * Time.deltaTime;

                // Asegurar que no sea menor que 0
                conquestProgress = Mathf.Max(conquestProgress, 0);

                // Ocultar slider si el progreso llega a 0
                if (conquestProgress <= 0 && conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
                {
                    GameEvents.RaiseBuildingCaptureFailed();

                    conquestSlider.gameObject.SetActive(false);
                    spriteRenderer.color = neutralColor;
                }
            }

            // Actualizar valor del slider
            if (conquestSlider != null)
            {
                conquestSlider.value = conquestProgress / conquestTime;
            }

            // Actualizar color según el progreso
            UpdateConquestColor();

            // Comprobar si se completó la conquista
            if (conquestProgress >= conquestTime && !isConquered)
            {
                CompleteConquest();
                yield break;
            }

            yield return null;
        }
    }

    void UpdateConquestColor()
    {
        if (isConquered) return;

        if (conqueringPlayers.Count > 0)
        {
            // Color de conquista activa (amarillo)
            spriteRenderer.color = conqueringColor;
        }
        else if (conquestProgress > 0)
        {
            // Color de conquista en pausa/decreciendo (naranja)
            spriteRenderer.color = Color.Lerp(neutralColor, conqueringColor, conquestProgress / conquestTime);
        }
        else
        {
            // Color neutral
            spriteRenderer.color = neutralColor;
        }
    }

    // Método para que los jugadores se registren en este edificio
    public void RegisterPlayer(PlayerBuildingDetector player)
    {
        if (!conqueringPlayers.Contains(player))
        {
            GameEvents.RaiseBuildingCaptureStarted();
            conqueringPlayers.Add(player);
            Debug.Log($"[{gameObject.name}] Jugador registrado. Total: {conqueringPlayers.Count}");
        }
    }

    // Método para que los jugadores se desregistren de este edificio
    public void UnregisterPlayer(PlayerBuildingDetector player)
    {
        if (conqueringPlayers.Contains(player))
        {
            conqueringPlayers.Remove(player);
            Debug.Log($"[{gameObject.name}] Jugador removido. Total: {conqueringPlayers.Count}");
        }
    }

    private void CompleteConquest()
    {
        GameEvents.RaiseBuildingCaptureCompleted();
        GameEvents.RaiseBuildingCaptured();
        isConquered = true;
        spriteRenderer.color = conqueredColor;

        if (conquestSlider != null)
        {
            conquestSlider.gameObject.SetActive(false);
        }

        // Limpiar lista de jugadores
        conqueringPlayers.Clear();

        StartMoneyGeneration();
        Debug.Log($"[{gameObject.name}] ¡EDIFICIO CONQUISTADO!");
    }

    private void StartMoneyGeneration()
    {
        if (moneyGenerationCoroutine == null)
        {
            moneyGenerationCoroutine = StartCoroutine(GenerateMoney());
        }
    }

    private IEnumerator GenerateMoney()
    {
        while (isConquered)
        {
            yield return new WaitForSeconds(moneyInterval);
            MoneyManager.Instance.AddMoney(moneyPerInterval);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isConquered ? Color.green : (conqueringPlayers.Count > 0 ? Color.yellow : Color.red);
        Gizmos.DrawWireSphere(transform.position, conquestRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + sliderOffset, 0.1f);
    }
}