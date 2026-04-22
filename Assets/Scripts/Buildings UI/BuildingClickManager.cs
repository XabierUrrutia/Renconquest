using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingClickManager : MonoBehaviour
{
    public Camera cam;
    public LayerMask layerMaskEdificios;

    private PlayerBaseBuildingClickHandler1 cachedPlayerBase;
    private readonly List<Collider2D> _hitBuffer = new List<Collider2D>();
    private readonly ContactFilter2D _noFilter = new ContactFilter2D();

    void Start()
    {
        if (cam == null) cam = Camera.main;
        _noFilter.NoFilter();
        cachedPlayerBase = FindObjectOfType<PlayerBaseBuildingClickHandler1>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (BuildingManager.Instance != null && BuildingManager.Instance.IsPlacing()) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(worldPos.x, worldPos.y);

        // ContactFilter.NoFilter() bypasa capas, triggers y cualquier filtro
        _hitBuffer.Clear();
        Physics2D.OverlapPoint(point, _noFilter, _hitBuffer);

        foreach (Collider2D col in _hitBuffer)
        {
            EdificioClick ec = col.GetComponentInParent<EdificioClick>();
            if (ec != null && ec.enabled)
            {
                ec.OnClicked();
                return;
            }

            PlayerBaseBuildingClickHandler1 pb = col.GetComponentInParent<PlayerBaseBuildingClickHandler1>();
            if (pb != null && pb.enabled)
            {
                pb.TogglePanel();
                return;
            }
        }

        // Fallback: comprueba el collider del PlayerBase directamente por si su layer
        // (FogOfWar) no fue devuelto por el query anterior
        if (cachedPlayerBase != null && cachedPlayerBase.enabled)
        {
            Collider2D col = cachedPlayerBase.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(point))
            {
                cachedPlayerBase.TogglePanel();
            }
        }
    }
}
