using UnityEngine;
using UnityEngine.UI; // Necesario para Slider

public class EnemyFogVisibility : MonoBehaviour
{
    private FogOfWar fogOfWar;
    private SpriteRenderer spriteRenderer;
    private Slider healthBar; // Referencia al slider de vida
    private Canvas healthBarCanvas; // Referencia al canvas si existe
    private bool wasVisible = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        fogOfWar = FindObjectOfType<FogOfWar>();

        // Buscar el slider de vida en los hijos o en el objeto
        healthBar = GetComponentInChildren<Slider>(true);

        // Si el health bar está en un Canvas World Space
        healthBarCanvas = GetComponentInChildren<Canvas>(true);

        if (fogOfWar == null)
        {
            Debug.LogError("No se encontró el sistema FogOfWar en la escena");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (fogOfWar == null) return;

        bool isCurrentlyVisible = fogOfWar.IsPositionVisible(transform.position);

        if (isCurrentlyVisible != wasVisible)
        {
            UpdateVisibility(isCurrentlyVisible);
            wasVisible = isCurrentlyVisible;
        }
    }

    void UpdateVisibility(bool isVisible)
    {
        // Ocultar/mostrar el sprite del enemigo
        spriteRenderer.enabled = isVisible;

        // Ocultar/mostrar la barra de vida
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(isVisible);
        }

        // Ocultar/mostrar el canvas completo si existe
        if (healthBarCanvas != null)
        {
            healthBarCanvas.enabled = isVisible;
        }
    }

    void OnBecameVisible()
    {
        // Backup en caso de que el renderer se active por otros medios
        if (!fogOfWar.IsPositionVisible(transform.position))
        {
            UpdateVisibility(false);
        }
    }
}