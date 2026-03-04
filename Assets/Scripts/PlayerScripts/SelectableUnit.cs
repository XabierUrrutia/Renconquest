using UnityEngine;

[DisallowMultipleComponent]
public class SelectableUnit : MonoBehaviour
{
    public bool isPlayerUnit = true;
    public GameObject arrowPrefab;
    public Vector3 arrowLocalOffset = new Vector3(0f, 0f, 0f);

    private GameObject arrowInstance;
    private SoldierTooltipTarget tooltipTarget;

    // Ahora esta variable puede estar vacía sin dar error
    private UnitVeterancy myVeterancy;

    void Awake()
    {
        myVeterancy = GetComponent<UnitVeterancy>();

        // --- CAMBIO: Ya no lanzamos error si falta la veterancia ---
        if (myVeterancy == null)
        {
            // Opcional: Solo un aviso suave o nada.
            // Debug.Log($"La unidad '{name}' no tiene sistema de Veterancia (Es normal para Tanques/Vehículos).");
        }
        // -----------------------------------------------------------

        if (isPlayerUnit)
        {
            var existing = GetComponentInChildren<FloatingArrow>(true);
            if (existing != null)
            {
                arrowInstance = existing.gameObject;
                arrowInstance.SetActive(false);
                arrowInstance.transform.SetParent(transform, true);
                arrowInstance.transform.localPosition = arrowLocalOffset;
            }
            else if (arrowPrefab != null)
            {
                arrowInstance = Instantiate(arrowPrefab, transform);
                arrowInstance.name = "SelectionArrow";
                arrowInstance.transform.localPosition = arrowLocalOffset;
                arrowInstance.SetActive(false);
            }
        }
        tooltipTarget = GetComponent<SoldierTooltipTarget>();
    }

    public void ShowSelection(bool show)
    {
        // 1. Mostrar/Ocultar flecha
        if (isPlayerUnit && arrowInstance != null) arrowInstance.SetActive(show);

        // 2. Mostrar Tooltip si existe
        if (tooltipTarget != null) tooltipTarget.ShowInfo(show);

        // 3. COMUNICACIÓN CON EL HUD (Protegida)
        if (show)
        {
            if (UnitHUDManager.Instance != null)
            {
                // CAMBIO: Solo llamamos al HUD si tenemos datos de veterancia que mostrar
                if (myVeterancy != null)
                {
                    UnitHUDManager.Instance.SeleccionarUnidad(myVeterancy);
                }
                else
                {
                    // Si es un tanque sin veterancia, decidimos si limpiar el HUD o no hacer nada
                    // De momento no hacemos nada para que no pete.
                }
            }
        }
    }

    void OnDestroy()
    {
        if (arrowInstance != null)
        {
            if (Application.isPlaying) Destroy(arrowInstance);
            else DestroyImmediate(arrowInstance);
        }
    }
}