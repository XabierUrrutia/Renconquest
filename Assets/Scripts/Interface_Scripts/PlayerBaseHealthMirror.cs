using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerBaseHealthMirror : MonoBehaviour
{
    [Header("Tag do Slider original da Player Base")]
    [Tooltip("Tag usada no Slider de HP original da PlayerBase (no prefab instanciado).")]
    public string playerBaseHpSliderTag = "PlayerBaseHPBar";

    [Header("Origem (fallback)")]
    [Tooltip("Referência direta à PlayerBase. Se vazio, tenta encontrar por tag 'PlayerBase' ou por tipo.")]
    public PlayerBase playerBase;

    [Header("Destino (este slider mirror)")]
    public Slider hpSliderMirror;

    [Header("Resolução automática")]
    [Tooltip("Intervalo em segundos entre tentativas de localizar Slider/PlayerBase em runtime.")]
    public float resolveInterval = 0.5f;

    [Header("Cores do Slider (MIRROR)")]
    [Tooltip("Cor quando em bom estado (> 250 HP)")]
    public Color healthyColor = Color.green;
    [Tooltip("Cor quando em aviso (<= 250 HP e > 30% da vida máxima)")]
    public Color warningColor = Color.yellow;
    [Tooltip("Cor quando crítico (<= 30% da vida máxima)")]
    public Color criticalColor = Color.red;

    [Header("Debug")]
    public bool showDebugLogs = true; // ATIVA PARA TESTAR

    private float _nextResolveTime;
    private Slider _sourceHpSlider;
    private Image _fillImage;
    private bool _isInitialized = false;
    private int _lastHp = -1;

    private void Start()
    {
        if (hpSliderMirror != null)
        {
            hpSliderMirror.minValue = 0f;

            if (hpSliderMirror.fillRect != null)
                _fillImage = hpSliderMirror.fillRect.GetComponent<Image>();
            if (_fillImage == null)
                _fillImage = hpSliderMirror.GetComponentInChildren<Image>();

            if (showDebugLogs)
            {
                if (_fillImage != null)
                    Debug.Log($"[Mirror] ✓ Fill Image encontrada: {_fillImage.name}");
                else
                    Debug.LogError("[Mirror] ✗ ERRO: Fill Image NÃO encontrada!");
            }
        }

        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f);

        ResolveBindings(true);

        if (playerBase != null)
        {
            playerBase.ForceInitializeHealth();

            int hp = playerBase.GetCurrentHealth();
            int maxHp = playerBase.GetMaxHealth();

            if (showDebugLogs)
            {
                Debug.Log($"[Mirror] PlayerBase encontrada! HP={hp}/{maxHp}");
            }

            if (hp > 0)
            {
                _isInitialized = true;
                if (showDebugLogs)
                    Debug.Log($"[Mirror] ✓ Inicializado! HP={hp}/{maxHp}");
                UpdateUI();
            }
            else
            {
                Debug.LogError($"[Mirror] ✗ ERRO: HP=0 após ForceInit! maxHealth={maxHp}");
            }
        }
        else
        {
            Debug.LogError("[Mirror] ✗ ERRO: PlayerBase NÃO encontrada!");
        }
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        if (Time.unscaledTime >= _nextResolveTime)
        {
            ResolveBindings(false);
            _nextResolveTime = Time.unscaledTime + resolveInterval;
        }

        UpdateUI();
    }

    private void ResolveBindings(bool logIfNotFound)
    {
        if (_sourceHpSlider == null && !string.IsNullOrEmpty(playerBaseHpSliderTag))
        {
            GameObject sliderObj = GameObject.FindGameObjectWithTag(playerBaseHpSliderTag);
            if (sliderObj != null)
                _sourceHpSlider = sliderObj.GetComponent<Slider>();
        }

        if (playerBase == null)
        {
            GameObject taggedBase = GameObject.FindGameObjectWithTag("PlayerBase");
            if (taggedBase != null)
                playerBase = taggedBase.GetComponent<PlayerBase>();

            if (playerBase == null)
                playerBase = FindObjectOfType<PlayerBase>();
        }
    }

    private void UpdateUI()
    {
        int currentHp = 0;
        int maxHp = 0;

        if (_sourceHpSlider != null && hpSliderMirror != null)
        {
            if (!Mathf.Approximately(hpSliderMirror.maxValue, _sourceHpSlider.maxValue))
                hpSliderMirror.maxValue = _sourceHpSlider.maxValue;

            hpSliderMirror.value = _sourceHpSlider.value;

            if (playerBase != null)
            {
                currentHp = playerBase.GetCurrentHealth();
                maxHp = playerBase.GetMaxHealth();

                if (_lastHp != currentHp)
                {
                    _lastHp = currentHp;
                    if (showDebugLogs)
                        Debug.Log($"[Mirror] HP mudou: {currentHp}/{maxHp}");
                }

                UpdateHealthBarColor(currentHp, maxHp);
            }

            return;
        }

        if (playerBase == null || hpSliderMirror == null)
            return;

        currentHp = playerBase.GetCurrentHealth();
        maxHp = playerBase.GetMaxHealth();

        if (_lastHp != currentHp)
        {
            _lastHp = currentHp;
            if (showDebugLogs)
                Debug.Log($"[Mirror] HP mudou: {currentHp}/{maxHp}");
        }

        if (!Mathf.Approximately(hpSliderMirror.maxValue, maxHp))
            hpSliderMirror.maxValue = maxHp;

        hpSliderMirror.value = currentHp;
        UpdateHealthBarColor(currentHp, maxHp);
    }

    /// <summary>
    /// LÓGICA EXATA do PlayerBase.UpdateHealthBarColor()
    /// </summary>
    private void UpdateHealthBarColor(int currentHealth, int maxHealth)
    {
        if (_fillImage == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[Mirror] _fillImage é null!");
            return;
        }

        // EXATAMENTE A MESMA LÓGICA DO PlayerBase.cs
        float criticalThreshold = maxHealth * 0.3f; // 30% = 300 se max=1000

        Color targetColor;
        string state = "";

        if (currentHealth <= criticalThreshold)
        {
            // VERMELHO: HP <= 300 (se max=1000)
            targetColor = criticalColor;
            state = "VERMELHO (crítico)";
        }
        else if (currentHealth <= 250)
        {
            // AMARELO: HP <= 250 mas > 300
            targetColor = warningColor;
            state = "AMARELO (aviso)";
        }
        else
        {
            // VERDE: HP > 250
            targetColor = healthyColor;
            state = "VERDE (saudável)";
        }

        // Aplicar a cor
        _fillImage.color = targetColor;

        // Log detalhado
        if (showDebugLogs)
        {
            Debug.Log($"[Mirror] 🎨 COR APLICADA: {state} | HP={currentHealth} | Crítico<={criticalThreshold:F0} | RGB=({targetColor.r:F2},{targetColor.g:F2},{targetColor.b:F2})");
        }
    }
}