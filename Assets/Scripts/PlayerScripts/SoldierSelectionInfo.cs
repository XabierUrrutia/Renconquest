using UnityEngine;

/// <summary>
/// Quando este soldado é selecionado, mostra o painel de info (SoldierTooltipManager)
/// com os dados de HP e tipo de soldado. Pode coexistir com o sistema da seta.
/// </summary>
[DisallowMultipleComponent]
public class SoldierSelectionInfo : MonoBehaviour
{
    [Header("Dados do soldado")]
    [Tooltip("Nome ou tipo de soldado (Infanteria, Arquero, etc.).")]
    public string soldierTypeName = "Soldier";

    [Tooltip("Componente de vida que implementa IHealth (por ex. PlayerHealth). Se vazio, será procurado no mesmo GameObject.")]
    public MonoBehaviour healthComponent;

    private IHealth health;

    private static SelectableUnit currentSelected;

    private void Awake()
    {
        if (healthComponent == null)
            healthComponent = GetComponent<MonoBehaviour>();

        if (healthComponent is IHealth h)
            health = h;
        else
            health = GetComponent<IHealth>();

        if (health == null)
        {
            Debug.LogWarning($"[SoldierSelectionInfo] Nenhum IHealth encontrado em '{gameObject.name}'. Painel vai mostrar só o tipo.");
        }
    }

    /// <summary>
    /// Chamar este método quando a unidade for selecionada (por ex. a partir de SelectableUnit.ShowSelection(true)).
    /// </summary>
    public void ShowSoldierInfo()
    {
        if (SoldierTooltipManager.Instance == null)
            return;

        int currentHp = 0;
        int maxHp = 0;

        if (health != null)
        {
            currentHp = health.GetCurrentHealth();
            maxHp = health.GetMaxHealth();
        }

        int currentAmmo = 0;
        int maxAmmo = 0;

        SoldierTooltipManager.Instance.ShowTooltip(
            soldierTypeName,
            currentHp,
            maxHp,
            currentAmmo,
            maxAmmo,
            transform.position);

        var veterania = GetComponent<UnitVeterancy>();

        if (UnitHUDManager.Instance != null)
        {
            // Le decimos al HUD global: "Muestra los datos de ESTE soldado"
            UnitHUDManager.Instance.SeleccionarUnidad(veterania);
        }
    }

    void SelectUnit(SelectableUnit unit)
    {
        // desseleciona anterior
        if (currentSelected != null)
            currentSelected.ShowSelection(false);

        currentSelected = unit;

        if (currentSelected != null)
            currentSelected.ShowSelection(true);
    }

    // Se já não precisares de clique direto no soldado, podes remover ou deixar vazio OnMouseDown
    // e deixar apenas o sistema de seleção global chamar ShowSelection/ShowSoldierInfo.
}