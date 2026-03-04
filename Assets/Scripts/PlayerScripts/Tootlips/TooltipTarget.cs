using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Coloca este componente em qualquer botão/UI para mostrar um tooltip
/// quando o rato fica por cima sem clicar.
/// Cada botão pode ter o seu próprio painel de tooltip (child) associado.
/// </summary>
[DisallowMultipleComponent]
public class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    [Tooltip("Texto que será mostrado no tooltip quando o rato estiver sobre este botão.")]
    public string tooltipText;

    [Tooltip("Painel de tooltip específico deste botão (GameObject). " +
             "Se vazio, será procurado num filho chamado 'TooltipPanel'.")]
    public GameObject localTooltipPanel;

    [Tooltip("Texto dentro do painel (opcional). Se vazio, será procurado em filhos de localTooltipPanel.")]
    public TextMeshProUGUI localTooltipText;

    [Tooltip("Se true, o tooltip aparece com um pequeno atraso.")]
    public bool useDelay = true;

    [Tooltip("Atraso em segundos antes de mostrar o tooltip.")]
    public float showDelay = 0.3f;

    private bool pointerInside;
    private float pointerEnterTime;
    private bool hasInitialized;

    void Awake()
    {
        InitLocalTooltip();
    }

    void InitLocalTooltip()
    {
        if (hasInitialized)
            return;

        hasInitialized = true;

        // Se não foi atribuído, procurar um filho chamado "TooltipPanel"
        if (localTooltipPanel == null)
        {
            Transform child = transform.Find("TooltipPanel");
            if (child != null)
                localTooltipPanel = child.gameObject;
        }

        if (localTooltipPanel != null)
        {
            // Tenta encontrar um TMP text dentro do painel
            if (localTooltipText == null)
                localTooltipText = localTooltipPanel.GetComponentInChildren<TextMeshProUGUI>(true);

            // Garante que começa escondido
            localTooltipPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!useDelay || !pointerInside || string.IsNullOrEmpty(tooltipText))
            return;

        if (Time.unscaledTime - pointerEnterTime >= showDelay)
        {
            ShowNow();
            // evita chamar múltiplas vezes
            pointerInside = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(tooltipText))
            return;

        InitLocalTooltip();

        pointerInside = true;
        pointerEnterTime = Time.unscaledTime;

        if (!useDelay)
            ShowNow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;

        // Esconde o painel local deste botão
        if (localTooltipPanel != null)
            localTooltipPanel.SetActive(false);

        // Se ainda usares TooltipsManager global, também o podes esconder
        if (TooltipsManager.Instance != null)
            TooltipsManager.Instance.HideTooltip();
    }

    private void ShowNow()
    {
        InitLocalTooltip();

        // 1) Preferir painel local por botão
        if (localTooltipPanel != null)
        {
            if (localTooltipText != null)
                localTooltipText.text = tooltipText;

            localTooltipPanel.SetActive(true);
            return;
        }

        // 2) Fallback: usar TooltipsManager global, se existir
        if (TooltipsManager.Instance != null)
            TooltipsManager.Instance.ShowTooltip(tooltipText);
    }
}