using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class MissionTriggerZone : MonoBehaviour
{
    [Header("Notifier destino")]
    [Tooltip("Qué MissionNotifier recibe el mensaje. Déjalo vacío para usar el Instance global (historia).")]
    public MissionNotifier targetNotifier;

    [Header("Modo")]
    [Tooltip("Coge el siguiente mensaje de la cola del notifier en vez de usar el texto de abajo.")]
    public bool useNextFromQueue = false;

    [Header("Mensaje (solo si useNextFromQueue está desactivado)")]
    public string missionTitle = "Objetivo";
    [TextArea(2, 4)]
    public string missionDescription = "Descripción del objetivo.";
    [Tooltip("Interrumpe el mensaje actual en lugar de encolar.")]
    public bool priority = false;

    [Header("Detección")]
    [Tooltip("Tag del objeto que activa la zona. Vacío = cualquier objeto.")]
    public string triggerTag = "Player";
    public bool triggerOnce = true;
    public bool completeOnExit = false;

    [Header("Condición (opcional)")]
    [Tooltip("Si se asigna, la zona solo se activa si ese GameObject ESTÁ activo en la escena.")]
    public GameObject conditionObjectActive;
    [Tooltip("Si se asigna, la zona solo se activa si ese GameObject NO está activo.")]
    public GameObject conditionObjectInactive;

    [Header("Editor")]
    public Color gizmoColor = new Color(0.1f, 0.9f, 0.4f, 0.22f);

    private bool _triggered = false;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[MissionTriggerZone] '{name}': collider no era trigger, activado automáticamente.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && _triggered) return;
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
        if (!ConditionMet()) return;

        MissionNotifier notifier = GetNotifier();
        if (notifier == null) return;

        _triggered = true;

        if (useNextFromQueue)
            notifier.TriggerNextQueued();
        else if (priority)
            notifier.ShowMissionPriority(missionTitle, missionDescription);
        else
            notifier.EnqueueMission(missionTitle, missionDescription);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!completeOnExit) return;
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;

        MissionNotifier notifier = GetNotifier();
        notifier?.CompleteCurrentMission();
    }

    bool ConditionMet()
    {
        if (conditionObjectActive != null && !conditionObjectActive.activeInHierarchy)
            return false;
        if (conditionObjectInactive != null && conditionObjectInactive.activeInHierarchy)
            return false;
        return true;
    }

    MissionNotifier GetNotifier() => targetNotifier != null ? targetNotifier : MissionNotifier.Instance;

    public void ResetTrigger() => _triggered = false;

    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Color outline = gizmoColor; outline.a = 1f;
        Gizmos.color = outline;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;
        Vector3 labelPos = col.bounds.center + Vector3.up * (col.bounds.extents.y + 0.3f);
        string label = useNextFromQueue ? "(siguiente de cola)" : (string.IsNullOrEmpty(missionTitle) ? "(sin título)" : missionTitle);
        Handles.Label(labelPos, $"🎯 {label}");
    }
#endif
}
