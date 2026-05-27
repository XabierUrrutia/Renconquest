using UnityEngine;
using UnityEngine.UI;

public class TutorialArrow : MonoBehaviour
{
    [Header("Pulse")]
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.15f;

    [Header("Point toward target (optional)")]
    public Transform target;
    public float rotationOffset = 0f;

    [Header("Bounce (optional)")]
    public float bounceDistance = 0.3f;
    public float bounceSpeed = 3f;

    [Header("Auto-hide on click")]
    public Button[] hideOnClick;

    [Header("Auto-hide on game events")]
    public bool hideOnBuildingPlaced = false;
    public bool hideOnSoldierRecruited = false;

    [Header("Linked panel (optional)")]
    [Tooltip("If assigned, the arrow hides when this panel closes and reappears when it opens (only if step not done).")]
    public GameObject linkedPanel;

    [Header("Chain: show next arrows when this one hides")]
    public TutorialArrow[] showOnHide;

    [Header("Follow world object (optional)")]
    [Tooltip("If assigned, the arrow moves to the screen position of this world Transform each frame.")]
    public Transform followWorldTarget;
    [Tooltip("Pixel offset applied on top of the world-to-screen position.")]
    public Vector2 followOffset = new Vector2(0f, 60f);

    private bool _done = false;
    private Vector3 baseScale;
    private Vector3 baseLocalPos;
    private float elapsed;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    void Awake()
    {
        baseScale = transform.localScale;
        baseLocalPos = transform.localPosition;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    void OnEnable()
    {
        elapsed = 0f;
        baseScale = transform.localScale;
        baseLocalPos = transform.localPosition;

        foreach (var btn in hideOnClick)
            if (btn != null) btn.onClick.AddListener(OnButtonClicked);

        if (hideOnBuildingPlaced)
            GameEvents.OnBuildingPlaced += OnBuildingPlacedHide;

        if (hideOnSoldierRecruited)
            GameEvents.OnSoldierRecruited += OnHideEvent;
    }

    void OnDisable()
    {
        foreach (var btn in hideOnClick)
            if (btn != null) btn.onClick.RemoveListener(OnButtonClicked);

        GameEvents.OnBuildingPlaced   -= OnBuildingPlacedHide;
        GameEvents.OnSoldierRecruited -= OnHideEvent;
    }

    void Update()
    {
        // If linked panel is closed, hide the arrow (without marking done)
        if (linkedPanel != null && !linkedPanel.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        // Follow a world-space object by converting to screen/canvas position
        if (followWorldTarget != null && rectTransform != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(followWorldTarget.position) + (Vector3)followOffset;
                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentCanvas.transform as RectTransform, screenPos,
                        parentCanvas.worldCamera, out Vector2 localPoint);
                    rectTransform.anchoredPosition = localPoint;
                }
                else
                {
                    rectTransform.position = screenPos;
                }
                baseLocalPos = rectTransform.localPosition;
            }
        }

        elapsed += Time.deltaTime;

        float s = 1f + Mathf.Sin(elapsed * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * s;

        if (target != null)
        {
            Vector2 dir = (Vector2)(target.position - transform.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (bounceDistance > 0f)
        {
            float b = Mathf.Sin(elapsed * bounceSpeed) * bounceDistance;
            Vector3 dir = target != null
                ? (target.position - transform.position).normalized
                : (Vector3)transform.right;
            transform.localPosition = baseLocalPos + dir * b;
        }
    }

    void OnButtonClicked()              => TriggerHide();
    void OnHideEvent()                  => TriggerHide();
    void OnBuildingPlacedHide(Transform _) => TriggerHide();

    void TriggerHide()
    {
        _done = true;
        foreach (var arrow in showOnHide)
            if (arrow != null) arrow.Show();
        Hide();
    }

    public void Show()
    {
        if (_done) return;
        gameObject.SetActive(true);
    }

    public void Hide()               => gameObject.SetActive(false);
    public void SetTarget(Transform t) => target = t;
    public void ResetDone()          => _done = false;
}