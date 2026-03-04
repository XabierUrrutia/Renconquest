using UnityEngine;
using UnityEngine.UI;

public class BrightnessControl : MonoBehaviour
{
    public Slider sliderBrillo;

    // Ya no hace falta que arrastres esto, lo buscaremos por código
    private Image panelNegroDetectado;

    void Start()
    {
        // 1. INTENTAR ENCONTRAR EL PANEL EN LA OTRA ESCENA
        // Buscamos cualquier objeto que tenga el script 'BrightnessReader'
        BrightnessReader lector = FindObjectOfType<BrightnessReader>();

        if (lector != null)
        {
            panelNegroDetectado = lector.GetComponent<Image>();
        }

        // 2. Cargar valor guardado
        float brilloGuardado = PlayerPrefs.GetFloat("NivelBrillo", 1.0f);
        sliderBrillo.SetValueWithoutNotify(brilloGuardado);

        // 3. Si encontramos el panel, lo actualizamos ya
        if (panelNegroDetectado != null)
        {
            AplicarColor(brilloGuardado);
        }

        sliderBrillo.onValueChanged.AddListener(AlMoverSlider);
    }

    public void AlMoverSlider(float valor)
    {
        // Guardar
        PlayerPrefs.SetFloat("NivelBrillo", valor);
        PlayerPrefs.Save();

        // Aplicar visualmente al panel de la otra escena
        AplicarColor(valor);
    }

    void AplicarColor(float valor)
    {
        // Solo intentamos cambiar el color si hemos encontrado el panel
        if (panelNegroDetectado != null)
        {
            float alpha = 1.0f - valor;
            Color c = panelNegroDetectado.color;
            c.a = alpha;
            panelNegroDetectado.color = c;
        }
    }
}