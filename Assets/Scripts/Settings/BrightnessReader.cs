using UnityEngine;
using UnityEngine.UI;

public class BrightnessReader : MonoBehaviour
{
    // Usamos OnEnable en vez de Awake/Start.
    // Esto se ejecuta CADA VEZ que el objeto se activa o la escena se muestra.
    void OnEnable()
    {
        ActualizarBrillo();
    }

    public void ActualizarBrillo()
    {
        Image miImagen = GetComponent<Image>();

        if (miImagen != null)
        {
            float brillo = PlayerPrefs.GetFloat("NivelBrillo", 1.0f);
            float alpha = 1.0f - brillo;

            Color c = miImagen.color;
            c.a = alpha;
            miImagen.color = c;
        }
    }
}