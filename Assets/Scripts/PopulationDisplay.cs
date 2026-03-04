using UnityEngine;
using TMPro;

public class PopulationDisplay : MonoBehaviour
{
    public TextMeshProUGUI textoSoldados;
    public TextMeshProUGUI textoTanques;

    // Usamos Update para que sea IMPOSIBLE que falle. 
    // Si la variable cambia, el texto cambiará al instante.
    void Update()
    {
        if (PopulationManager.Instance != null)
        {
            if (textoSoldados != null)
                textoSoldados.text = $"{PopulationManager.Instance.soldadosActuales}/{PopulationManager.Instance.maxSoldados}";

            if (textoTanques != null)
                textoTanques.text = $"{PopulationManager.Instance.tanquesActuales}/{PopulationManager.Instance.maxTanques}";
        }
    }
}