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
        healthBar = GetComponentInChildren<Slider>(true);
        healthBarCanvas = GetComponentInChildren<Canvas>(true);

        // Si no hay FogOfWar simplemente desactivar el script
        // sin dar error — todo visible por defecto
        if (fogOfWar == null)
        {
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
        // Proteger contra null si no hay FogOfWar
        if (fogOfWar == null) return;

        if (!fogOfWar.IsPositionVisible(transform.position))
        {
            UpdateVisibility(false);
        }
    }
}