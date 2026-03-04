using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI da "loja" de buildings:
/// - Liga-se ao MoneyManager para saber o dinheiro atual.
/// - Mostra apenas como COMPRÁVEIS os buildings que o jogador pode pagar.
/// - Para os que não pode comprar: botão fica desativado e aparece um ícone de "cadeado".
/// </summary>
public class BuildingShopUI : MonoBehaviour
{
    [System.Serializable]
    public class BuildingEntryUI
    {
        [Tooltip("Nome amigável do building (apenas para UI).")]
        public string displayName;

        [Tooltip("Custo em dinheiro deste building.")]
        public int cost = 10;

        [Header("Referências UI")]
        public GameObject root;               // painel/linha inteira do building
        public Button buyButton;              // botão clicar para construir
        public TextMeshProUGUI nameText;      // nome do building (opcional)
        public TextMeshProUGUI costText;      // custo (opcional)

        [Tooltip("Ícone de cadeado que aparece quando não pode ser comprado.")]
        public GameObject lockIcon;

        [Tooltip("Imagem do building (opcional, pode ser cinzentada quando bloqueado).")]
        public Image buildingIcon;
    }

    [Header("Entries de UI")]
    [Tooltip("Lista de entries (um por tipo de building na loja).")]
    public BuildingEntryUI[] buildingEntries;

    [Header("Cores de estado (opcional)")]
    public Color availableColor = Color.white;
    public Color unavailableColor = Color.gray;

    void Awake()
    {
        RefreshAll();
    }

    void OnEnable()
    {
        RefreshAll();
    }

    void Update()
    {
        // opcional: atualizar todo frame; se quiser otimizar, pode chamar apenas quando o dinheiro mudar
        RefreshAll();
    }

    void RefreshAll()
    {
        if (buildingEntries == null)
            return;

        int currentMoney = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        foreach (var entry in buildingEntries)
        {
            if (entry == null || entry.root == null)
                continue;

            int cost = Mathf.Max(0, entry.cost);

            if (entry.nameText != null)
                entry.nameText.text = string.IsNullOrEmpty(entry.displayName) ? "Building" : entry.displayName;

            if (entry.costText != null)
                entry.costText.text = cost > 0 ? $"{cost}$" : "$0";

            bool canAfford = currentMoney >= cost && cost > 0;

            // botão só interage se puder comprar
            if (entry.buyButton != null)
                entry.buyButton.interactable = canAfford;

            // mostrar / esconder cadeado
            if (entry.lockIcon != null)
                entry.lockIcon.SetActive(!canAfford);

            // opcional: mudar cor do ícone quando não pode comprar
            if (entry.buildingIcon != null)
                entry.buildingIcon.color = canAfford ? availableColor : unavailableColor;
        }
    }

    /// <summary>
    /// Deve ser ligado no onClick do botão de cada building, ANTES do evento de colocar o building.
    /// Chama isto para tentar gastar o dinheiro; devolve se conseguiu ou não.
    /// </summary>
    public bool TrySpendForBuilding(int cost)
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[BuildingShopUI] MoneyManager.Instance é null.");
            return false;
        }

        if (cost <= 0)
            return true;

        if (!MoneyManager.Instance.SpendMoney(cost))
        {
            Debug.Log("[BuildingShopUI] Dinheiro insuficiente para comprar building.");
            return false;
        }

        // --- AÑADIDO: VERIFICAR DERROTA TRAS GASTO ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.VerificarDineroTrasGasto();
        }
        // ---------------------------------------------

        return true;
    }
}