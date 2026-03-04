using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EnemyBase : MonoBehaviour
{
    [Header("Configuración de Conquista")]
    public float conquestTime = 30f;
    public float conquestRange = 5f;

    [Header("Dependencias (Bloqueo)")]
    [Tooltip("Arrastra aquí las fábricas que deben ser conquistadas antes de atacar esta base.")]
    public EnemyBaseFactory[] requiredFactories; // <--- NUEVO: Array de fábricas

    [Header("Defensa (Horda Final)")]
    [Tooltip("Arrastra aquí los Spawners (Tanques, Soldados, etc.) que se activarán al atacar la base.")]
    public EnemySpawner[] defensiveSpawners; // <--- NUEVO: Array de spawners
    private bool defensiveHordeTriggered = false;

    [Header("Velocidades de Conquista")]
    public float growthSpeed = 1f;
    public float decaySpeed = 0.5f;

    [Header("UI Elements")]
    public Slider conquestSlider;
    public Vector3 sliderPosition = new Vector3(-1309.61f, -144.46f, 0f);
    public GameObject victoryCanvas;

    // Opcional: Icono visual para saber que está bloqueada
    public GameObject lockIcon;

    [Header("Estados")]
    public bool isConquered = false;
    public bool isLocked = true; // <--- NUEVO: Estado de bloqueo

    // Variables internas
    private float conquestProgress = 0f;
    private Canvas sliderCanvas;
    private List<PlayerBuildingDetector> conqueringPlayers = new List<PlayerBuildingDetector>();

    void Start()
    {
        InitializeBase();
        SetupSlider();

        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);

        // Chequeo inicial por si acaso no asignaste fábricas
        if (requiredFactories == null || requiredFactories.Length == 0) isLocked = false;
    }

    void InitializeBase()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = conquestRange;

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }
    }

    void SetupSlider()
    {
        if (conquestSlider != null)
        {
            // (Tu lógica original de configuración del slider se mantiene intacta)
            GameObject canvasGO = new GameObject("EnemyBaseCanvas");
            canvasGO.transform.SetParent(transform);
            sliderCanvas = canvasGO.AddComponent<Canvas>();
            sliderCanvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = sliderCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 2f);

            sliderCanvas.transform.position = sliderPosition;
            sliderCanvas.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            conquestSlider.transform.SetParent(sliderCanvas.transform);
            conquestSlider.minValue = 0f;
            conquestSlider.maxValue = 1f;
            conquestSlider.value = 0f;
            conquestSlider.gameObject.SetActive(false);

            conquestSlider.transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        // 1. NUEVO: Verificar si sigue bloqueada
        CheckLockStatus();

        // 2. Actualizar icono de candado (opcional)
        if (lockIcon != null) lockIcon.SetActive(isLocked && !isConquered);

        if (!isConquered)
        {
            UpdateConquestProgress();
        }
    }

    // --- NUEVO MÉTODO: Revisa si las fábricas han caído ---
    void CheckLockStatus()
    {
        if (!isLocked) return; // Si ya está desbloqueada, no hacemos nada

        bool allFactoriesConquered = true;
        if (requiredFactories != null)
        {
            foreach (var factory in requiredFactories)
            {
                // Si la fábrica existe y NO está conquistada, seguimos bloqueados
                if (factory != null && !factory.IsConquered())
                {
                    allFactoriesConquered = false;
                    break;
                }
            }
        }
        else
        {
            allFactoriesConquered = true;
        }

        if (allFactoriesConquered)
        {
            isLocked = false;
            Debug.Log("¡ESCUDO DE LA BASE DESACTIVADO! ¡A POR ELLOS!");
        }
    }

    // --- NUEVO MÉTODO: Activa la horda defensiva ---
    void TriggerDefensiveHorde()
    {
        defensiveHordeTriggered = true;
        Debug.Log("¡ALERTA ROJA! La base lanza sus defensas.");

        if (defensiveSpawners != null)
        {
            foreach (var spawner in defensiveSpawners)
            {
                if (spawner != null) spawner.StartSpawning();
            }
        }
    }

    void UpdateConquestProgress()
    {
        // MODIFICADO: Si hay jugadores intentando conquistar...
        if (conqueringPlayers.Count > 0)
        {
            // ...PERO la base está bloqueada
            if (isLocked)
            {
                // No sube el progreso. Aquí podrías poner un aviso en pantalla.
                // Debug.Log("Base Bloqueada: Conquista las fábricas primero.");
                return;
            }

            // ...Y está desbloqueada: Activamos la Horda si es la primera vez
            if (!defensiveHordeTriggered)
            {
                TriggerDefensiveHorde();
            }

            // Lógica normal de conquista
            float progressIncrement = (growthSpeed * conqueringPlayers.Count) / conquestTime;
            conquestProgress += progressIncrement * Time.deltaTime;
            conquestProgress = Mathf.Min(conquestProgress, conquestTime);

            if (conquestProgress > 0 && conquestSlider != null && !conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(true);
            }
        }
        else if (conquestProgress > 0) // Nadie conquista, baja el progreso
        {
            float progressDecrement = decaySpeed / conquestTime;
            conquestProgress -= progressDecrement * Time.deltaTime;
            conquestProgress = Mathf.Max(conquestProgress, 0);

            if (conquestProgress <= 0 && conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
            {
                GameEvents.RaiseBuildingCaptureFailed();

                conquestSlider.gameObject.SetActive(false);
            }
        }

        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
        {
            conquestSlider.value = conquestProgress / conquestTime;
        }

        if (conquestProgress >= conquestTime && !isConquered)
        {
            CompleteConquest();
        }
    }

    // Mantengo tus métodos de registro intactos
    public void RegisterPlayer(PlayerBuildingDetector player)
    {
        if (!conqueringPlayers.Contains(player))
        {
            GameEvents.RaiseBuildingCaptureStarted();

            conqueringPlayers.Add(player);
        }
    }

    public void UnregisterPlayer(PlayerBuildingDetector player)
    {
        if (conqueringPlayers.Contains(player))
        {
            conqueringPlayers.Remove(player);
        }
    }

    private void CompleteConquest()
    {
        
        isConquered = true;
        GameEvents.RaiseBuildingCaptureCompleted();
        GameEvents.RaiseBuildingCaptured();

        if (conquestSlider != null)
        {
            conquestSlider.gameObject.SetActive(false);
        }

        conqueringPlayers.Clear();
        ActivateVictoryCanvas();

        Debug.Log("¡BASE ENEMIGA CONQUISTADA!");
    }

    private void ActivateVictoryCanvas()
    {
        Time.timeScale = 0f;

        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);
            Debug.Log("Canvas de victoria activado - JUEGO PAUSADO");
        }
        else
        {
            Debug.LogWarning("Victory Canvas no asignado en EnemyBase");
        }
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    void OnDrawGizmosSelected()
    {
        // Visualización del estado: Negro=Bloqueada, Rojo=Atacable, Azul=Conquistada
        Gizmos.color = isLocked ? Color.black : (isConquered ? Color.blue : (conqueringPlayers.Count > 0 ? Color.yellow : Color.red));
        Gizmos.DrawWireSphere(transform.position, conquestRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(sliderPosition, 0.5f);
    }

    void OnDestroy()
    {
        if (sliderCanvas != null)
        {
            Destroy(sliderCanvas.gameObject);
        }
    }
}