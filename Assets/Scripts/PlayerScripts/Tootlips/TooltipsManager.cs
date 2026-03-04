using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class TooltipsManager : MonoBehaviour
{
    public static TooltipsManager Instance { get; private set; }

    [Header("Referências")]
    [Tooltip("Painel raiz do tooltip (GameObject que contém o fundo e o texto).")]
    public GameObject tooltipPanel;

    [Tooltip("Componente TextMeshProUGUI que mostra o texto do tooltip.")]
    public TextMeshProUGUI tooltipText;

    [Tooltip("Offset em pixels a partir da posição do rato.")]
    public Vector2 mouseOffset = new Vector2(16f, -16f);

    private RectTransform panelRect;
    private Canvas canvas;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tooltipPanel != null)
            panelRect = tooltipPanel.GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf && panelRect != null && canvas != null)
        {
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out mousePos);

            panelRect.anchoredPosition = mousePos + mouseOffset;
        }
    }

    public void ShowTooltip(string text)
    {
        if (tooltipPanel == null || tooltipText == null)
            return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel == null)
            return;

        tooltipPanel.SetActive(false);
    }
}