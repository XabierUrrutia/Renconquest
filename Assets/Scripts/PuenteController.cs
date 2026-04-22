using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Ponlo en el GameObject del puente.
/// Desactiva el puente (waypoints + tilemap visual) hasta que las fábricas requeridas sean conquistadas.
/// </summary>
public class PuenteController : MonoBehaviour
{
    [Header("Fábricas Requeridas")]
    [Tooltip("El puente se activa cuando TODAS estas fábricas sean conquistadas.")]
    public EnemyBaseFactory[] fabricasRequeridas;

    [Header("Waypoints del Puente")]
    [Tooltip("Arrastra aquí todos los WaypointPuente de este puente.")]
    public WaypointPuente[] waypoints;

    [Header("Visual del Puente (Tilemap)")]
    [Tooltip("Arrastra aquí el Tilemap visual del puente.")]
    public Tilemap tilemapPuente;

    [Tooltip("Collider del Tilemap si tiene uno separado (opcional).")]
    public TilemapCollider2D tilemapCollider;

    [Header("Estado")]
    public bool puenteActivo = false;

    private bool yaActivado = false;

    void Start()
    {
        // Si no hay fábricas requeridas, el puente empieza activo
        if (fabricasRequeridas == null || fabricasRequeridas.Length == 0)
        {
            ActivarPuente();
            return;
        }

        // Empezar desactivado
        SetEstadoPuente(false);
    }

    void Update()
    {
        if (yaActivado) return;

        Debug.Log($"[Puente] Comprobando... yaActivado={yaActivado}");
        foreach (var f in fabricasRequeridas)
        {
            if (f != null)
                Debug.Log($"[Puente] Fabrica {f.name} conquistada: {f.IsConquered()}");
        }

        if (TodasFabricasConquistadas())
            ActivarPuente();
    }

    private bool TodasFabricasConquistadas()
    {
        foreach (var fabrica in fabricasRequeridas)
        {
            if (fabrica != null && !fabrica.IsConquered())
                return false;
        }
        return true;
    }

    private void ActivarPuente()
    {
        yaActivado = true;
        puenteActivo = true;
        SetEstadoPuente(true);
        Debug.Log($"[PuenteController] Puente '{name}' activado.");
    }

    private void SetEstadoPuente(bool activo)
    {
        // Activar/desactivar waypoints
        if (waypoints != null)
        {
            foreach (var wp in waypoints)
            {
                if (wp != null)
                    wp.gameObject.SetActive(activo);
            }
        }

        // Activar/desactivar tilemap visual
        if (tilemapPuente != null)
        {
            tilemapPuente.gameObject.SetActive(activo);
        }

        // Activar/desactivar collider si es separado
        if (tilemapCollider != null)
        {
            tilemapCollider.enabled = activo;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (fabricasRequeridas == null) return;

        Gizmos.color = puenteActivo ? Color.green : Color.red;
        foreach (var fabrica in fabricasRequeridas)
        {
            if (fabrica != null)
                Gizmos.DrawLine(transform.position, fabrica.transform.position);
        }
    }
}