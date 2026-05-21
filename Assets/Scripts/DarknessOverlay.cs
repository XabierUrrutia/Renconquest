using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay de oscuridad fija para el tercer mapa.
/// Ponlo en un Image negro en el Canvas con alpha fijo.
/// No interfiere con el ajuste de brillo del jugador.
/// </summary>
public class DarknessOverlay : MonoBehaviour
{
    [Tooltip("Nivel de oscuridad base (0 = transparente, 1 = negro total)")]
    [Range(0f, 1f)]
    public float darknessLevel = 0.4f;

    void Start()
    {
        Image img = GetComponent<Image>();
        if (img != null)
            img.color = new Color(0f, 0f, 0f, darknessLevel);
    }
}