using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handler de clique para a PLAYER BASE.
/// - REQUER um Collider2D no MESMO GameObject (Is Trigger DESMARCADO).
/// - Ao clicar com o rato em cima da base, abre/fecha o painel da PlayerBase.
/// - Permite gastar dinheiro para curar a vida (HP) da PlayerBase.
/// - NÃO congela o jogo ao abrir o painel.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PlayerBaseBuildingClickHandler1 : MonoBehaviour
{
    [Header("UI da Player Base")]
    [Tooltip("Painel de UI específico da PlayerBase (GameObject na cena)")]
    public GameObject panelPlayerBaseUI;

    [Tooltip("Texto que mostra a vida atual / máxima da base")]
    public TextMeshProUGUI hpText;

    [Tooltip("Slider que mostra visualmente a vida da base (opcional)")]
    public Slider hpSlider;

    [Header("Cura / Custo")]
    [Tooltip("HP curado por utilização do botão 'Heal'")]
    public int healAmount = 50;

    [Tooltip("Custo em dinheiro por utilização de cura")]
    public int healCost = 20;

    [Tooltip("Botão da UI que executa a cura")]
    public Button healButton;

    private PlayerBase playerBase;
    private Collider2D buildingCollider;

    void Awake()
    {
        buildingCollider = GetComponent<Collider2D>();
        if (buildingCollider == null)
        {
            Debug.LogError($"[PlayerBaseBuildingClickHandler1] '{gameObject.name}' NÃO TEM Collider2D.");
            enabled = false;
            return;
        }

        if (buildingCollider.isTrigger)
        {
            Debug.LogWarning($"[PlayerBaseBuildingClickHandler1] '{gameObject.name}' tem Collider2D como Trigger. " +
                             "Para OnMouseDown funcionar melhor, DESMARCA 'Is Trigger'.");
        }

        playerBase = GetComponent<PlayerBase>();
        if (playerBase == null)
        {
            Debug.LogError($"[PlayerBaseBuildingClickHandler1] '{gameObject.name}' não tem componente PlayerBase.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // REMOVER blocos que mexem no box.offset e box.size
        // Apenas deixamos a lógica de UI.

        // tentar encontrar painel automaticamente se não foi ligado no Inspector
        if (panelPlayerBaseUI == null)
        {
            var panel = FindObjectOfType<PanelPlayerBaseUI>(true);
            if (panel != null)
            {
                panelPlayerBaseUI = panel.gameObject;
                Debug.Log($"[PlayerBaseBuildingClickHandler1] PanelPlayerBaseUI encontrado automaticamente: {panelPlayerBaseUI.name}");
            }
        }

        if (panelPlayerBaseUI != null)
            panelPlayerBaseUI.SetActive(false);

        if (healButton == null && panelPlayerBaseUI != null)
        {
            healButton = panelPlayerBaseUI.GetComponentInChildren<Button>(true);
        }

        if (healButton != null)
        {
            healButton.onClick.RemoveAllListeners();
            healButton.onClick.AddListener(OnHealButtonClicked);
        }

        UpdateUI();
    }

    void OnMouseDown()
    {
        Debug.Log($"[PlayerBaseBuildingClickHandler1] OnMouseDown em '{gameObject.name}'");

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[PlayerBaseBuildingClickHandler1] Clique ignorado: está sobre UI.");
            return;
        }

        // DEBUG: ver qual collider está a ser atingido pelo rato
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 point = new Vector2(worldPos.x, worldPos.y);
            RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log($"[PlayerBaseBuildingClickHandler1] Raycast atingiu '{hit.collider.name}' na posição {point}");
            }
            else
            {
                Debug.Log($"[PlayerBaseBuildingClickHandler1] Raycast NÃO atingiu nada na posição {point}");
            }
        }

        TogglePanel();
    }

    public void TogglePanel()
    {
        if (panelPlayerBaseUI == null)
        {
            Debug.LogError("[PlayerBaseBuildingClickHandler1] panelPlayerBaseUI não atribuído.");
            return;
        }

        bool open = !panelPlayerBaseUI.activeSelf;

        if (open)
        {
            panelPlayerBaseUI.SetActive(true);
            UpdateUI();
            Debug.Log("[PlayerBaseBuildingClickHandler1] Painel da PlayerBase ABERTO.");
        }
        else
        {
            panelPlayerBaseUI.SetActive(false);
            Debug.Log("[PlayerBaseBuildingClickHandler1] Painel da PlayerBase FECHADO.");
        }
    }

    void OnHealButtonClicked()
    {
        SoundColector.Instance?.PlayUiClick();
        if (playerBase == null)
            return;

        if (playerBase.GetCurrentHealth() >= playerBase.GetMaxHealth())
        {
            Debug.Log("[PlayerBaseBuildingClickHandler1] Base já está com HP máximo. Cura ignorada.");
            UpdateUI();
            return;
        }

        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[PlayerBaseBuildingClickHandler1] MoneyManager.Instance é null. Não é possível curar.");
            return;
        }

        if (MoneyManager.Instance.CurrentMoney < healCost)
        {
            Debug.Log("[PlayerBaseBuildingClickHandler1] Dinheiro insuficiente para curar a base.");
            UpdateUI();
            return;
        }

        MoneyManager.Instance.SpendMoney(healCost);
        playerBase.Heal(healAmount);

        Debug.Log($"[PlayerBaseBuildingClickHandler1] Base curada em {healAmount} HP por {healCost} moedas.");
        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerBase == null)
            return;

        int hp = playerBase.GetCurrentHealth();
        int maxHp = playerBase.GetMaxHealth();

        if (hpText != null)
            hpText.text = $"{hp} / {maxHp}";

        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHp;
            hpSlider.value = hp;
        }

        if (healButton != null)
        {
            bool canHeal =
                hp < maxHp &&
                MoneyManager.Instance != null &&
                MoneyManager.Instance.CurrentMoney >= healCost;

            healButton.interactable = canHeal;
        }
    }

    public void RefreshUI()
    {
        UpdateUI();
    }

    void OnDisable()
    {
        if (panelPlayerBaseUI != null && panelPlayerBaseUI.activeSelf)
            panelPlayerBaseUI.SetActive(false);
    }
}