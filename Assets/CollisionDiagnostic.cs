using UnityEngine;

/// <summary>
/// Pon este script en el soldado para diagnosticar por qué no colisiona.
/// ELIMÍNALO cuando el problema esté resuelto.
/// </summary>
public class CollisionDiagnostic : MonoBehaviour
{
    private Rigidbody2D rb;
    private CapsuleCollider2D col;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        Debug.Log("=== DIAGNÓSTICO SOLDADO ===");

        if (rb == null)
            Debug.LogError($"[{name}] ❌ NO tiene Rigidbody2D");
        else
        {
            Debug.Log($"[{name}] ✅ Rigidbody2D encontrado");
            Debug.Log($"[{name}]    Body Type: {rb.bodyType}");
            Debug.Log($"[{name}]    Collision Detection: {rb.collisionDetectionMode}");
            Debug.Log($"[{name}]    Simulated: {rb.simulated}");
        }

        if (col == null)
            Debug.LogError($"[{name}] ❌ NO tiene CapsuleCollider2D");
        else
        {
            Debug.Log($"[{name}] ✅ CapsuleCollider2D encontrado");
            Debug.Log($"[{name}]    isTrigger: {col.isTrigger}");
            Debug.Log($"[{name}]    Size: {col.size}");
            Debug.Log($"[{name}]    Offset: {col.offset}");
            Debug.Log($"[{name}]    Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }

        // Buscar edificios cercanos
        Collider2D[] cercanos = Physics2D.OverlapCircleAll(transform.position, 10f);
        Debug.Log($"[{name}] Colliders en radio 10: {cercanos.Length}");
        foreach (var c in cercanos)
        {
            if (c.gameObject != gameObject)
                Debug.Log($"[{name}]    → '{c.gameObject.name}' Layer: {LayerMask.LayerToName(c.gameObject.layer)} | isTrigger: {c.isTrigger} | RB: {(c.attachedRigidbody != null ? c.attachedRigidbody.bodyType.ToString() : "ninguno")}");
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        Debug.Log($"[{name}] ✅ COLISIÓN con: {col.gameObject.name} (Layer: {LayerMask.LayerToName(col.gameObject.layer)})");
    }

    void OnCollisionStay2D(Collision2D col)
    {
        Debug.Log($"[{name}] Mantiene colisión con: {col.gameObject.name}");
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        Debug.Log($"[{name}] ⚠️ TRIGGER (no bloquea) con: {col.gameObject.name} (Layer: {LayerMask.LayerToName(col.gameObject.layer)})");
    }
}