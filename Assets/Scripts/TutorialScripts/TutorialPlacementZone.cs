using UnityEngine;

public class TutorialPlacementZone : MonoBehaviour
{
    [Header("Visual")]
    public SpriteRenderer zoneSprite;
    public Color zoneColor = new Color(0f, 1f, 0.5f, 0.35f);
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.08f;

    [Header("Placement radius")]
    public float radius = 2f;

    private Vector3 baseScale;
    private float elapsed;

    public static TutorialPlacementZone Active { get; private set; }

    void OnEnable()
    {
        Active = this;
        elapsed = 0f;
        if (zoneSprite != null)
        {
            zoneSprite.color = zoneColor;
            float diameter = radius * 2f;
            zoneSprite.transform.localScale = Vector3.one * diameter;
            baseScale = zoneSprite.transform.localScale;
        }
    }

    void OnDisable()
    {
        if (Active == this) Active = null;
    }

    void Update()
    {
        if (zoneSprite == null) return;
        elapsed += Time.deltaTime;
        float s = 1f + Mathf.Sin(elapsed * pulseSpeed) * pulseAmount;
        zoneSprite.transform.localScale = baseScale * s;
    }

    public bool Contains(Vector3 worldPos)
    {
        return Vector2.Distance(new Vector2(worldPos.x, worldPos.y),
                                new Vector2(transform.position.x, transform.position.y)) <= radius;
    }

    public void ShowAt(Vector3 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.z);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
