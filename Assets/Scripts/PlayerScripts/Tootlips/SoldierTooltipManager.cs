using UnityEngine;
using TMPro;

/// <summary>
/// Manager global de info de soldados.
/// - Deve existir uma instância na Canvas (HUD).
/// - Recebe pedidos de SoldierSelectionInfo para mostrar info do soldado selecionado.
/// - Usa um único prefab/painel de UI para todos os soldados.
/// </summary>
[DisallowMultipleComponent]
public class SoldierTooltipManager : MonoBehaviour
{
    public static SoldierTooltipManager Instance { get; private set; }

    [Header("Referências UI")]
    [Tooltip("Painel raiz do tooltip de soldado (o teu prefab instanciado na Canvas).")]
    public GameObject tooltipPanel;

    [Tooltip("Texto de nome/tipo do soldado.")]
    public TextMeshProUGUI nameText;

    [Tooltip("Texto de HP do soldado.")]
    public TextMeshProUGUI hpText;

    [Tooltip("Texto de munições do soldado (opcional).")]
    public TextMeshProUGUI ammoText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void ShowTooltip(
        string soldierType,
        int currentHp,
        int maxHp,
        int currentAmmo,
        int maxAmmo,
        Vector3 worldPos)
    {
        if (tooltipPanel == null)
            return;

        if (nameText != null)
            nameText.text = soldierType;

        if (hpText != null)
        {
            if (maxHp > 0)
                hpText.text = $"HP: {currentHp} / {maxHp}";
            else
                hpText.text = $"HP: {currentHp}";
        }

        if (ammoText != null)
        {
            if (maxAmmo > 0)
                ammoText.text = $"Ammo: {currentAmmo} / {maxAmmo}";
            else
                ammoText.text = "Ammo: -";
        }

        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}