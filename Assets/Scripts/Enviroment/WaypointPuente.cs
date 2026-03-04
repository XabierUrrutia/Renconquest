using UnityEngine;

public class WaypointPuente : MonoBehaviour
{
    [Header("Conexión del Puente")]
    public WaypointPuente waypointConectado;

    [Header("Configuración Visual")]
    public Color colorGizmo = Color.cyan;
    public float radioGizmo = 0.5f;

    [Header("Detección de Lados")]
    public string ladoRio = "Este"; // "Este", "Oeste", "Norte", "Sur" - según tu mapa

    void OnDrawGizmos()
    {
        // Dibujar el waypoint
        Gizmos.color = colorGizmo;
        Gizmos.DrawWireSphere(transform.position, radioGizmo);

        // Dibujar conexión si existe
        if (waypointConectado != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, waypointConectado.transform.position);

            // Flecha direccional
            Vector3 direccion = (waypointConectado.transform.position - transform.position).normalized;
            Vector3 perpendicular = new Vector3(-direccion.y, direccion.x, 0) * 0.2f;
            Vector3 cabezaFlecha = waypointConectado.transform.position - direccion * 0.3f;

            Gizmos.DrawLine(cabezaFlecha, cabezaFlecha - direccion * 0.5f + perpendicular);
            Gizmos.DrawLine(cabezaFlecha, cabezaFlecha - direccion * 0.5f - perpendicular);
        }

        // Etiqueta con nombre y lado
#if UNITY_EDITOR
        GUIStyle estilo = new GUIStyle();
        estilo.normal.textColor = colorGizmo;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, $"{name} ({ladoRio})", estilo);
#endif
    }

    public bool EstaConectado()
    {
        return waypointConectado != null;
    }

    // Nuevo método para verificar si dos puntos están en el mismo lado
    public bool MismoLadoQue(Vector3 posicion, float radioDeteccion = 3f)
    {
        // Verificar por proximidad física
        float distancia = Vector3.Distance(transform.position, posicion);
        return distancia <= radioDeteccion;
    }
}