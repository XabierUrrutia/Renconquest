using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI da "loja" de soldados:
/// - Liga-se ao MoneyManager para saber o dinheiro atual.
/// - Mostra apenas como RECRUTÁVEIS os soldados que o jogador pode pagar.
/// - Para os que não pode recrutar: botão fica desativado e aparece um ícone de "cadeado".
/// - NÃO executa o recrutamento diretamente: o próprio Button continua com o onClick
///   já configurado no Inspector (ex.: chamar PanelSoldadosUI.OnRecruitSoldado / OnRecruitGeneral).
/// </summary>
public class SoldierShopUI : MonoBehaviour
{
    [System.Serializable]
    public class SoldierEntryUI
    {
        [Tooltip("Nome amigável do soldado (apenas para UI).")]
        public string displayName;

        [Tooltip("Custo em dinheiro deste tipo de soldado.")]
        public int cost = 10;

        [Header("Referências UI")]
        public GameObject root;               // painel/linha inteira do soldado
        public Button recruitButton;          // botão para recrutar
        public TextMeshProUGUI nameText;      // nome do soldado (opcional)
        public TextMeshProUGUI costText;      // custo (opcional)

        [Tooltip("Ícone de cadeado que aparece quando não pode ser recrutado.")]
        public GameObject lockIcon;

        [Tooltip("Imagem do soldado (opcional, pode ser cinzentada quando bloqueado).")]
        public Image soldierIcon;
    }

    [Header("Entries de UI")]
    [Tooltip("Lista de entries (um por tipo de soldado na loja).")]
    public SoldierEntryUI[] soldierEntries;

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
        if (soldierEntries == null)
            return;

        int currentMoney = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        foreach (var entry in soldierEntries)
        {
            if (entry == null || entry.root == null)
                continue;

            int cost = Mathf.Max(0, entry.cost);

            if (entry.nameText != null)
                entry.nameText.text = string.IsNullOrEmpty(entry.displayName) ? "Soldado" : entry.displayName;

            if (entry.costText != null)
                entry.costText.text = cost > 0 ? $"{cost}$" : "$0";

            bool canAfford = currentMoney >= cost && cost > 0;

            // botão só interage se puder recrutar
            if (entry.recruitButton != null)
                entry.recruitButton.interactable = canAfford;

            // mostrar / esconder cadeado
            if (entry.lockIcon != null)
                entry.lockIcon.SetActive(!canAfford);

            // mudar cor do ícone quando não pode recrutar
            if (entry.soldierIcon != null)
                entry.soldierIcon.color = canAfford ? availableColor : unavailableColor;
        }
    }

    /// <summary>
    /// Deve ser ligado no onClick do botão de cada soldado, ANTES do evento de recrutar.
    /// Ex.: no botão do soldado, primeiro chama SoldierShopUI.TrySpendForSoldier(custo),
    /// depois, se true, chama PanelSoldadosUI.OnRecruitSoldado/OnRecruitGeneral.
    /// </summary>
    public bool TrySpendForSoldier(int cost)
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[SoldierShopUI] MoneyManager.Instance é null.");
            return false;
        }

        if (cost <= 0)
            return true;

        if (!MoneyManager.Instance.SpendMoney(cost))
        {
            Debug.Log("[SoldierShopUI] Dinheiro insuficiente para recrutar soldado.");
            return false;
        }

        return true;
    }
}