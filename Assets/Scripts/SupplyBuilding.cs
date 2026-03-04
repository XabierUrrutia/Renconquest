using UnityEngine;

public class SupplyBuilding : MonoBehaviour
{
    [Header("Configuración de Suministros")]
    public PopulationManager.TipoUnidad tipoQueAumenta;
    public int cantidadQueSuma = 10;

    void Start()
    {
        if (PopulationManager.Instance != null)
        {
            PopulationManager.Instance.AumentarCapacidad(tipoQueAumenta, cantidadQueSuma);
            // DEBUG PARA VER SI FUNCIONA
            Debug.Log($"[EDIFICIO] {name} construido. +{cantidadQueSuma} capacidad.");
        }
    }

    void OnDestroy()
    {
        // --- CORRECCIÓN CLAVE ---
        // Si este script estaba desactivado (era un fantasma/preview),
        // significa que nunca sumó nada, así que NO permitimos que reste.
        if (!this.enabled) return;
        // ------------------------

        if (PopulationManager.Instance != null && gameObject.scene.isLoaded)
        {
            PopulationManager.Instance.ReducirCapacidad(tipoQueAumenta, cantidadQueSuma);
        }
    }
}