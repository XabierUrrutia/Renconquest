using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // Obliga a que el objeto tenga una imagen
public class BrightnessApplier : MonoBehaviour
{
    void Start()
    {
        // 1. Buscamos el objeto Image de este mismo panel
        Image overlay = GetComponent<Image>();

        // 2. Leemos el valor guardado (o 1 por defecto si es la primera vez)
        float savedBrightness = PlayerPrefs.GetFloat("MasterBrightness", 1.0f);

        // 3. Calculamos la transparencia (Invertido: 1 brillo = 0 opacidad)
        float alpha = 1.0f - savedBrightness;

        // 4. Aplicamos el color negro con esa transparencia
        Color color = Color.black;
        color.a = alpha;
        overlay.color = color;
    }
}