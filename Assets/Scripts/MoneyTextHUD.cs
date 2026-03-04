using UnityEngine;
using TMPro;

/// <summary>
/// Muestra el dinero total, los ingresos estimados y los gastos estimados.
/// </summary>
[DisallowMultipleComponent]
public class MoneyTextHUD : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Texto grande del dinero total.")]
    public TextMeshProUGUI moneyText;

    [Tooltip("Texto para mostrar ingresos (ej: +25/s).")]
    public TextMeshProUGUI incomeText;

    [Tooltip("Texto para mostrar gastos (ej: -10/s).")]
    public TextMeshProUGUI expensesText;

    [Header("Configuración")]
    [Tooltip("Actualizar a cada frame? Útil si los valores cambian muy rápido.")]
    public bool updateEveryFrame = false;

    void Awake()
    {
        // Si no se asigna manualmente, intentamos buscarlo en el mismo objeto
        if (moneyText == null)
            moneyText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        if (updateEveryFrame)
        {
            Refresh();
        }
    }

    /// <summary>
    /// Actualiza todos los textos consultando al MoneyManager.
    /// </summary>
    public void Refresh()
    {
        if (MoneyManager.Instance == null) return;

        // 1. Dinero Actual
        if (moneyText != null)
        {
            moneyText.text = $"{MoneyManager.Instance.CurrentMoney}";
        }

        // 2. Ingresos (Verde)
        if (incomeText != null)
        {
            int ingresos = MoneyManager.Instance.CalcularIngresosPorSegundo();
            // Formato: +25/s
            incomeText.text = $"+{ingresos}/s";
            // Opcional: Forzar color verde si se pierde
            // incomeText.color = Color.green; 
        }

        // 3. Gastos (Rojo)
        if (expensesText != null)
        {
            int gastos = MoneyManager.Instance.CalcularGastosPorTick();
            // Formato: -10/s
            if (gastos > 0)
                expensesText.text = $"-{gastos}/s";
            else
                expensesText.text = "-0/s";

            // Opcional: Forzar color rojo
            // expensesText.color = Color.red;
        }
    }
}