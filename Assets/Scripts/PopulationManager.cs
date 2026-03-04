using UnityEngine;
using System;

public class PopulationManager : MonoBehaviour
{
    public static PopulationManager Instance { get; private set; }

    [Header("Población: Soldados")]
    public int soldadosActuales = 0;
    public int maxSoldados = 10;

    [Header("Población: Tanques")]
    public int tanquesActuales = 0;
    public int maxTanques = 0;

    public event Action OnPopulationChanged;

    public enum TipoUnidad { Soldado, Tanque }

    void Awake()
    {
        Instance = this;
    }

    // --- GESTIÓN DE EDIFICIOS (Capacidad) ---
    // (Esto se queda igual, los edificios aumentan el MAX)
    public void AumentarCapacidad(TipoUnidad tipo, int cantidad)
    {
        if (tipo == TipoUnidad.Soldado) maxSoldados += cantidad;
        else if (tipo == TipoUnidad.Tanque) maxTanques += cantidad;

        OnPopulationChanged?.Invoke();
    }

    public void ReducirCapacidad(TipoUnidad tipo, int cantidad)
    {
        if (tipo == TipoUnidad.Soldado) maxSoldados -= cantidad;
        else if (tipo == TipoUnidad.Tanque) maxTanques -= cantidad;

        if (maxSoldados < 0) maxSoldados = 0;
        if (maxTanques < 0) maxTanques = 0;

        OnPopulationChanged?.Invoke();
    }

    // --- GESTIÓN DE UNIDADES (Ocupar Espacio Variable) ---

    // AHORA RECIBE "CANTIDAD"
    public bool HayEspacio(TipoUnidad tipo, int cantidadRequerida)
    {
        if (tipo == TipoUnidad.Soldado)
        {
            // ¿Caben estos X soldados nuevos?
            return (soldadosActuales + cantidadRequerida) <= maxSoldados;
        }
        else
        {
            return (tanquesActuales + cantidadRequerida) <= maxTanques;
        }
    }

    public void RegistrarUnidad(TipoUnidad tipo, int cantidad)
    {
        if (tipo == TipoUnidad.Soldado)
        {
            soldadosActuales += cantidad; // <--- ¡ASEGÚRATE DE QUE SEA UN MAS (+)!
        }
        else
        {
            tanquesActuales += cantidad;  // <--- ¡AQUÍ TAMBIÉN!
        }

        Debug.Log($"[POBLACIÓN] Unidad creada. Soldados: {soldadosActuales}/{maxSoldados}");
        OnPopulationChanged?.Invoke();
    }

    public void EliminarUnidad(TipoUnidad tipo, int cantidad)
    {
        if (tipo == TipoUnidad.Soldado)
        {
            soldadosActuales -= cantidad; // <--- ¡AQUÍ ES UN MENOS (-)!
        }
        else
        {
            tanquesActuales -= cantidad;
        }

        // Corrección de seguridad para que nunca baje de 0
        if (soldadosActuales < 0) soldadosActuales = 0;
        if (tanquesActuales < 0) tanquesActuales = 0;

        Debug.Log($"[POBLACIÓN] Unidad eliminada. Soldados: {soldadosActuales}/{maxSoldados}");
        OnPopulationChanged?.Invoke();
    }
}