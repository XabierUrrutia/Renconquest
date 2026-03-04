using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Inflación de Salarios")]
    public float costoMultiplicador = 1.0f;

    [Header("Configuración Económica")]
    public bool sePaganSalarios = false;

    [Header("Config")]
    public int startMoney = 10;

    [Header("Dinero del Jugador")]
    [SerializeField] // Para verlo en inspector pero que sea privado set
    private int currentMoney = 0;
    public int CurrentMoney { get { return currentMoney; } }

    [Header("Rendimiento")]
    public float incomeTickInterval = 1f;

    // --- LISTAS DE CONTROL ---
    private readonly List<BuildingOwnership> incomeSources = new List<BuildingOwnership>();
    private readonly List<EnemyBaseFactory> activeFactories = new List<EnemyBaseFactory>();
    private readonly List<UnitVeterancy> activeUnits = new List<UnitVeterancy>();

    private Coroutine incomeCoroutine;

    void Awake()
    {
        // Singleton Persistente
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializamos el valor la primera vez que se crea
        currentMoney = startMoney;
    }

    void Start()
    {
        if (incomeCoroutine == null)
            incomeCoroutine = StartCoroutine(IncomeTickRoutine());
    }

    // --- ESTA ES LA FUNCIÓN CLAVE QUE LLAMARÁ EL BUILDING MANAGER ---
    public void ResetMoney()
    {
        Debug.Log("[MoneyManager] Reseteando economía por inicio de nivel.");

        // 1. Resetear dinero al valor inicial
        currentMoney = startMoney;

        // 2. Limpiar listas de la partida anterior
        activeUnits.Clear();
        incomeSources.Clear();
        activeFactories.Clear();

        // 3. Resetear condiciones de victoria/derrota o reglas
        sePaganSalarios = false;
        costoMultiplicador = 1.0f;

        // 4. Actualizar HUD
        NotifyHUD();
    }
    // -------------------------------------------------------------

    public void NotifyHUD()
    {
        var hud = FindObjectOfType<MoneyTextHUD>();
        if (hud != null) hud.Refresh();
    }

    public void AddMoney(int amount)
    {
        if (amount == 0) return;
        currentMoney += amount;
        NotifyHUD();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            NotifyHUD();
            return true;
        }
        GameEvents.RaiseInsufficientResources();
        return false;
    }

    // --- REGISTROS ---

    public void RegisterIncomeSource(BuildingOwnership source)
    {
        if (source == null) return;
        if (!incomeSources.Contains(source)) incomeSources.Add(source);
        NotifyHUD();
    }

    public void UnregisterIncomeSource(BuildingOwnership source)
    {
        if (source == null) return;
        if (incomeSources.Contains(source)) incomeSources.Remove(source);
        NotifyHUD();
    }

    public void RegisterFactory(EnemyBaseFactory factory)
    {
        if (factory == null) return;
        if (!activeFactories.Contains(factory))
        {
            activeFactories.Add(factory);
            NotifyHUD();
        }
    }

    public void UnregisterFactory(EnemyBaseFactory factory)
    {
        if (factory == null) return;
        if (activeFactories.Contains(factory))
        {
            activeFactories.Remove(factory);
            NotifyHUD();
        }
    }

    public void RegisterUnitExpense(UnitVeterancy unit)
    {
        if (unit == null) return;
        if (!activeUnits.Contains(unit))
        {
            activeUnits.Add(unit);
            NotifyHUD();
        }
    }

    public void UnregisterUnitExpense(UnitVeterancy unit)
    {
        if (unit == null) return;
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
            NotifyHUD();
        }
    }

    // --- CÁLCULOS UI ---

    public int CalcularIngresosPorSegundo()
    {
        float totalIncomePerSecond = 0f;

        foreach (var source in incomeSources)
        {
            if (source != null) totalIncomePerSecond += source.incomePerTick;
        }

        foreach (var factory in activeFactories)
        {
            if (factory != null && factory.moneyInterval > 0)
            {
                totalIncomePerSecond += (factory.moneyPerInterval / factory.moneyInterval);
            }
        }

        return Mathf.RoundToInt(totalIncomePerSecond);
    }

    public int CalcularGastosPorTick()
    {
        if (!sePaganSalarios) return 0;

        int total = 0;
        foreach (var u in activeUnits)
        {
            if (u != null) total += u.CalcularMantenimiento();
        }
        return total;
    }

    // --- RUTINA PRINCIPAL ---
    IEnumerator IncomeTickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(incomeTickInterval);

            int passiveIncome = 0;
            // Limpieza y suma de ingresos
            for (int i = incomeSources.Count - 1; i >= 0; i--)
            {
                if (incomeSources[i] == null) incomeSources.RemoveAt(i);
                else passiveIncome += incomeSources[i].incomePerTick;
            }

            int totalUpkeep = 0;
            // Limpieza y suma de gastos
            if (sePaganSalarios)
            {
                for (int i = activeUnits.Count - 1; i >= 0; i--)
                {
                    if (activeUnits[i] == null) activeUnits.RemoveAt(i);
                    else totalUpkeep += activeUnits[i].CalcularMantenimiento();
                }
            }

            currentMoney += passiveIncome;

            if (currentMoney >= totalUpkeep)
            {
                currentMoney -= totalUpkeep;
            }
            else
            {
                currentMoney = 0;
                // GameEvents.RaiseBankrupcy(); // Opcional
            }

            NotifyHUD();
        }
    }

    void OnDestroy()
    {
        if (incomeCoroutine != null) StopCoroutine(incomeCoroutine);
    }

    public void ModificarInflacion(float cantidad)
    {
        costoMultiplicador += cantidad;
        NotifyHUD();
    }

    public void ActivarCobroDeSalarios()
    {
        if (!sePaganSalarios)
        {
            sePaganSalarios = true;
            NotifyHUD();
        }
    }
}