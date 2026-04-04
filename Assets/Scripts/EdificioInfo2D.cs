using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EdificioInfo2D : MonoBehaviour
{
    [Header("Configuración del Edificio")]
    public string nombreEscenaDestino;
    public string nombreEdificio = "Edificio";
    [TextArea(2, 3)]
    public string descripcionEdificio = "Haz click para entrar";

    [Header("Tooltip Isométrico")]
    public GameObject tooltipPrefab;
    public Vector3 offset = new Vector3(0, -2f, 0);
    public float escala = 0.015f;

    [Header("Texturas de Estado")]
    [Tooltip("SpriteRenderer que se pondrá encima del Tilemap para mostrar el estado")]
    public SpriteRenderer estadoRenderer;
    [Tooltip("Sprite cuando el nivel está bloqueado")]
    public Sprite spriteConCandado;
    [Tooltip("Sprite cuando el nivel está desbloqueado")]
    public Sprite spriteSinCandado;
    [Tooltip("Sprite cuando el nivel está completado (opcional)")]
    public Sprite spriteCompletado;

    private GameObject miTooltip;
    private Camera camara;

    [Header("Cutscene Video (opcional)")]
    [SerializeField] private VideoOverlayPlayer overlay;
    [SerializeField] private float skipInputDelay = 0.15f;
    private float skipBlockUntil = 0f;

    void Start()
    {
        camara = Camera.main;
        if (!overlay) overlay = FindObjectOfType<VideoOverlayPlayer>();

        if (tooltipPrefab != null)
            CrearTooltipIsometrico();

        ActualizarSprite();
    }

    // Llamado cada vez que se activa el GameObject (por si vuelves del nivel)
    void OnEnable()
    {
        ActualizarSprite();
    }

    void ActualizarSprite()
    {
        if (estadoRenderer == null || LevelManager.Instance == null) return;

        int level = LevelManager.Instance.GetLevelIndexByScene(nombreEscenaDestino);
        if (level == 0) return; // Esta escena no es un nivel gestionado

        bool unlocked = LevelManager.Instance.IsUnlocked(level);
        bool completed = LevelManager.Instance.IsCompleted(level);

        if (completed && spriteCompletado != null)
            estadoRenderer.sprite = spriteCompletado;
        else if (unlocked && spriteSinCandado != null)
            estadoRenderer.sprite = spriteSinCandado;
        else if (!unlocked && spriteConCandado != null)
            estadoRenderer.sprite = spriteConCandado;
    }

    void CrearTooltipIsometrico()
    {
        miTooltip = Instantiate(tooltipPrefab);

        Canvas canvas = miTooltip.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 100);
        }

        ActualizarTextoTooltip();
        miTooltip.SetActive(false);
    }

    void Update()
    {
        if (miTooltip != null && miTooltip.activeInHierarchy)
            ActualizarPosicionIsometrica();
    }

    void ActualizarPosicionIsometrica()
    {
        if (miTooltip == null) return;

        Vector3 posicionMundo = transform.position + offset;
        miTooltip.transform.position = posicionMundo;

        if (camara != null)
            miTooltip.transform.rotation = camara.transform.rotation;

        miTooltip.transform.localScale = Vector3.one * escala;
    }

    void ActualizarTextoTooltip()
    {
        if (miTooltip == null) return;

        TextMeshProUGUI[] textosTMP = miTooltip.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI texto in textosTMP)
        {
            if (texto.name.Contains("Nombre") || texto.gameObject.name.Contains("Nombre"))
                texto.text = nombreEdificio;
            else if (texto.name.Contains("Desc") || texto.gameObject.name.Contains("Desc"))
                texto.text = descripcionEdificio;
        }
    }

    void OnMouseEnter()
    {
        if (miTooltip != null)
        {
            miTooltip.SetActive(true);
            ActualizarPosicionIsometrica();
        }
    }

    void OnMouseExit()
    {
        if (miTooltip != null)
            miTooltip.SetActive(false);
    }

    void OnMouseDown()
    {
        if (string.IsNullOrEmpty(nombreEscenaDestino)) return;

        if (LevelManager.Instance != null)
        {
            bool unlocked = LevelManager.Instance.IsSceneUnlocked(nombreEscenaDestino);
            if (!unlocked)
            {
                Debug.Log($"[EdificioInfo2D] Level '{nombreEscenaDestino}' bloqueado.");
                ShowLockedTooltip();
                return;
            }
        }

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.ResetMoney();

        if (overlay != null) overlay.PlayDefaultAndLoadAsync(nombreEscenaDestino);
        else SceneManager.LoadScene(nombreEscenaDestino);
    }

    void ShowLockedTooltip()
    {
        if (miTooltip == null) return;

        TextMeshProUGUI[] textosTMP = miTooltip.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI texto in textosTMP)
        {
            if (texto.name.Contains("Desc") || texto.gameObject.name.Contains("Desc"))
                texto.text = "Bloqueado. Completa el nivel anterior primero.";
        }

        miTooltip.SetActive(true);
        ActualizarPosicionIsometrica();
        CancelInvoke(nameof(RestoreTooltipText));
        Invoke(nameof(RestoreTooltipText), 2f);
    }

    void RestoreTooltipText()
    {
        ActualizarTextoTooltip();
        if (miTooltip != null)
            miTooltip.SetActive(false);
    }

    void OnDestroy()
    {
        if (miTooltip != null)
            Destroy(miTooltip);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 posicionTooltip = transform.position + offset;
        Gizmos.DrawWireSphere(posicionTooltip, 0.3f);
        Gizmos.DrawLine(transform.position, posicionTooltip);
    }
}