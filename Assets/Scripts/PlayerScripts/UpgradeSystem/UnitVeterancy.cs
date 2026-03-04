using UnityEngine;
using System;

public class UnitVeterancy : MonoBehaviour
{
    // EVENTO: Avisa al mundo (y al HUD) que mis stats cambiaron
    public event Action OnStatsChanged;

    [Header("Economía (Mantenimiento)")]
    public int costeBase = 5;       // Coste a nivel 1
    public int costePorNivel = 2;   // Cuánto sube por cada nivel extra
    public int divisorDeCoste = 4;

    [Header("Identidad de la Unidad")]
    public Sprite retratoCara;

    [Header("Estado")]
    public int nivel = 1;
    public int xpActual = 0;
    public int xpParaSiguienteNivel = 100;

    [Header("Configuración")]
    public int maxNivel = 3;
    public int bonusSalud = 2;
    public int bonusDaño = 1;

    // Referencias internas
    private PlayerShooting shootingScript;
    private PlayerHealth healthScript;
    private SelectableUnit selectableScript;

    void Start()
    {
        shootingScript = GetComponent<PlayerShooting>();
        healthScript = GetComponent<PlayerHealth>();
        selectableScript = GetComponent<SelectableUnit>();
    }

    public void GanarXP(int cantidad)
    {
        if (nivel >= maxNivel) return;

        xpActual += cantidad;

        if (xpActual >= xpParaSiguienteNivel)
        {
            SubirNivel();
        }

        OnStatsChanged?.Invoke();
    }

    void SubirNivel()
    {
        xpActual -= xpParaSiguienteNivel;
        nivel++;
        xpParaSiguienteNivel = Mathf.RoundToInt(xpParaSiguienteNivel * 1.5f);

        if (shootingScript != null) shootingScript.bulletDamage += bonusDaño;
        if (healthScript != null) { healthScript.maxHealth += bonusSalud; healthScript.Revive(); }

        GameEvents.RaiseUnitUpgraded();

        // Al subir de nivel, aumenta el coste, notificamos al Manager si está activo
        if (MoneyManager.Instance != null) MoneyManager.Instance.NotifyHUD();

        Debug.Log($"{name} subió a nivel {nivel}");
    }

    public int CalcularMantenimiento()
    {
        // 1. Coste base de la unidad (ej: 5 + nivel*extra)
        float costeBaseTotal = costeBase + (nivel * costePorNivel);

        // El divisor reduce el coste para que no sea excesivo (ej: 5/4 = 1.25)
        float total = costeBaseTotal / divisorDeCoste;

        // 2. Aplicamos la Inflación Global del MoneyManager
        if (MoneyManager.Instance != null)
        {
            total *= MoneyManager.Instance.costoMultiplicador;
        }

        // 3. Redondeamos hacia arriba (para que 5.2 sea 6 monedas)
        return Mathf.CeilToInt(total);
    }

    void OnEnable()
    {
        // Al nacer o activarse, se apunta a la lista de gastos
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.RegisterUnitExpense(this);
    }

    void OnDisable()
    {
        // Al morir o desactivarse, se borra de la lista
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.UnregisterUnitExpense(this);
    }
}