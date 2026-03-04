using UnityEngine;
using TMPro;

public class WaveProgressUI : MonoBehaviour
{
    [Header("Elementos TextMeshPro")]
    public TextMeshProUGUI waveInfoText;
    public TextMeshProUGUI enemiesText;
    public TextMeshProUGUI timerText;

    [Header("Configuración")]
    public bool showWaveNumber = true;
    public bool showEnemiesCount = true;
    public bool showTimer = true;
    public float flashSpeed = 2f;
    public bool enablePulseEffect = true;

    [Header("Efectos")]
    public float pulseIntensity = 0.1f;
    public float pulseSpeed = 2f;

    private Color originalTimerColor;
    private float pulseTimer = 0f;
    private Vector3 originalWaveTextScale;

    void Start()
    {
        if (timerText != null)
            originalTimerColor = timerText.color;

        if (waveInfoText != null)
            originalWaveTextScale = waveInfoText.transform.localScale;

        UpdateUI();
    }

    void Update()
    {
        if (EnemyWaveManager.Instance == null) return;

        UpdateUI();
        UpdateEffects();
    }

    void UpdateUI()
    {
        if (EnemyWaveManager.Instance == null)
        {
            SetTexts("Sistema de Oleadas", "No disponible", "");
            return;
        }

        int waveNumber = EnemyWaveManager.Instance.GetCurrentWaveNumber();
        int enemies = EnemyWaveManager.Instance.GetActiveEnemiesCount();
        float timeLeft = EnemyWaveManager.Instance.GetTimeToNextWave();
        bool waveActive = EnemyWaveManager.Instance.IsWaveInProgress();
        bool revengeWave = EnemyWaveManager.Instance.IsRevengeWaveActive();

        // Texto principal de oleada
        string waveStr;
        if (revengeWave)
        {
            waveStr = "<color=#FF4444><b>Revenge</b></color>";
        }
        else
        {
            waveStr = showWaveNumber ? $"<b>Wave {waveNumber}</b>" : "<b>Wave</b>";
        }

        // Texto de enemigos
        string enemiesStr = "";
        if (showEnemiesCount)
        {
            enemiesStr = $"Enemies: {enemies}";
        }

        // Texto de tiempo
        string timeStr = "";
        if (showTimer)
        {
            if (waveActive)
            {
                timeStr = revengeWave ? "<color=#FF4444>¡DEFEND!</color>" : "In Progress";
            }
            else
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                timeStr = $"{seconds}s";
            }
        }

        SetTexts(waveStr, enemiesStr, timeStr);
    }

    void UpdateEffects()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;

        // Efecto de parpadeo para el timer
        if (timerText != null && showTimer)
        {
            float timeLeft = EnemyWaveManager.Instance.GetTimeToNextWave();
            if (timeLeft <= 10f && !EnemyWaveManager.Instance.IsWaveInProgress())
            {
                float alpha = Mathf.PingPong(Time.time * flashSpeed, 1f);
                timerText.color = Color.Lerp(originalTimerColor, Color.red, alpha);
            }
            else
            {
                timerText.color = originalTimerColor;
            }
        }

        // Efecto de pulso para el texto de oleada
        if (enablePulseEffect && waveInfoText != null && EnemyWaveManager.Instance.IsWaveInProgress())
        {
            float pulse = Mathf.Sin(pulseTimer) * pulseIntensity + 1f;
            waveInfoText.transform.localScale = originalWaveTextScale * pulse;
        }
    }

    void SetTexts(string waveInfo, string enemies, string timer)
    {
        if (waveInfoText != null)
            waveInfoText.text = waveInfo;

        if (enemiesText != null)
            enemiesText.text = enemies;

        if (timerText != null)
            timerText.text = timer;
    }

    public void ToggleDetailedInfo(bool detailed)
    {
        showEnemiesCount = detailed;
        showTimer = detailed;

        if (enemiesText != null)
            enemiesText.gameObject.SetActive(detailed);

        if (timerText != null)
            timerText.gameObject.SetActive(detailed);
    }
}