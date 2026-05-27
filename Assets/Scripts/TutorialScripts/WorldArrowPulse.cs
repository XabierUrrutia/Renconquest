using UnityEngine;

public class WorldArrowPulse : MonoBehaviour
{
    [Header("Pulse (scale)")]
    public float pulseSpeed  = 3f;
    public float pulseAmount = 0.15f;

    [Header("Bounce (position)")]
    public float bounceSpeed    = 3f;
    public float bounceDistance = 0.2f;

    // Set by the tutorial manager at spawn time
    [HideInInspector] public Transform followTarget;
    [HideInInspector] public Vector2   followOffset;

    Vector3 baseScale;
    float elapsed;

    void OnEnable()
    {
        baseScale = transform.localScale;
        elapsed   = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // Pulse scale
        float s = 1f + Mathf.Sin(elapsed * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * s;

        // Follow target + bounce
        Vector3 basePos = followTarget != null
            ? new Vector3(
                followTarget.position.x + followOffset.x,
                followTarget.position.y + followOffset.y,
                followTarget.position.z - 0.1f)
            : transform.position;

        float b = Mathf.Sin(elapsed * bounceSpeed) * bounceDistance;
        transform.position = basePos + Vector3.up * b;
    }
}
