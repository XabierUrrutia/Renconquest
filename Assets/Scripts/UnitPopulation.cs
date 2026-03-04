using UnityEngine;

public class UnitPopulation : MonoBehaviour
{
    public PopulationManager.TipoUnidad soyUnaUnidadDe;
    public int costePoblacion = 1;

    // Se ejecuta cada vez que el objeto se enciende (Nace o se Reactiva)
    void OnEnable()
    {
        if (PopulationManager.Instance != null)
        {
            PopulationManager.Instance.RegistrarUnidad(soyUnaUnidadDe, costePoblacion);
        }
    }

    // Se ejecuta cada vez que el objeto se apaga (Muere o se Desactiva)
    void OnDisable()
    {
        // Verificamos que el Manager siga existiendo (por si cierras el juego)
        if (PopulationManager.Instance != null && gameObject.scene.isLoaded)
        {
            PopulationManager.Instance.EliminarUnidad(soyUnaUnidadDe, costePoblacion);
        }
    }
}