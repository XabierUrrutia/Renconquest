using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Base e área de construção")]
    public Transform militaryBase;
    [Tooltip("Raio onde é permitida a construção (unidades do mundo)")]
    public float buildRadius = 10f;

    [Header("Layers e validação")]
    [Tooltip("Layers que bloqueiam a colocação (ex.: Ground, Buildings)")]
    public LayerMask blockingLayers;
    public float placeZ = 0f;

    [Header("Preview")]
    public Color previewValidColor = new Color(0f, 0.55f, 1f, 0.6f);
    public Color previewInvalidColor = new Color(1f, 0f, 0f, 0.6f);
    public Color constructionColor = new Color(1f, 0.9f, 0.5f, 0.9f);

    [Header("Input / comportamento")]
    [Tooltip("0 = left, 1 = right, 2 = middle")]
    public int placementMouseButton = 1; // right click
    public bool cancelWithEsc = true;
    public bool ignoreWhenPointerOverUI = true;

    [Header("Grid snap")]
    public bool enableGridSnap = true;
    public Vector2 defaultGridSize = Vector2.one;

    [Header("Recursos")]
    public bool requireResources = true;

    [Header("Fog of War")]
    [Tooltip("Referencia ao FogOfWar (se vazio tenta encontrar na cena)")]
    public FogOfWar fogOfWar;

    [Header("Construção - Animator")]
    [Tooltip("Nome do parâmetro bool no Animator para marcar 'under construction' (se existir)")]
    public string animatorUnderConstructionBool = "UnderConstruction";
    [Tooltip("Pulso fallback (se não houver Animator)")]
    public float constructionPulseScale = 0.06f;
    public float constructionPulseSpeed = 3.5f;

    // runtime
    private BuildingData selectedBuildingData;
    private GameObject currentPrefab;
    private GameObject previewInstance;
    private bool isPlacing = false;
    private Camera mainCam;

    // caches
    private Dictionary<Renderer, Color> previewOriginalColors = new Dictionary<Renderer, Color>();
    private List<Material> previewInstantiatedMaterials = new List<Material>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogWarning($"BuildingManager: instância duplicada em '{name}', destruindo componente.");
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        mainCam = Camera.main;
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();

        // Resetear dinero al iniciar nivel
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.ResetMoney();
        }
    }

    void Update()
    {
        // Si no estamos colocando nada, salir
        if (!isPlacing || currentPrefab == null) return;

        // Evitar pausa
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // --- 1. MOVER EL GHOST (SIEMPRE) ---
        Vector3 mouseWorld = GetMouseWorldPosition();

        if (previewInstance != null)
        {
            Vector3 targetPos = mouseWorld;

            // Grid Snap
            if (enableGridSnap && selectedBuildingData != null)
            {
                Vector2 grid = selectedBuildingData.gridSize;
                if (grid == Vector2.zero) grid = defaultGridSize;
                targetPos = ApplyGridSnap(targetPos, grid);
            }

            previewInstance.transform.position = targetPos;

            // Validar posición (Rojo/Verde)
            bool valid = IsValidPlacement(targetPos, out Collider2D[] overlapping);
            UpdatePreviewVisual(valid);

            // --- 2. CONFIRMAR CONSTRUCCIÓN (CLIC IZQUIERDO) ---
            if (Input.GetMouseButtonDown(0)) // 0 es botón izquierdo
            {
                // Si haces clic sobre la UI, no construir
                if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                if (valid)
                {
                    int cost = selectedBuildingData != null ? selectedBuildingData.cost : 0;

                    // Intentar cobrar
                    if (!requireResources || TrySpendResources(cost))
                    {
                        // Construir
                        GameObject placed = Instantiate(currentPrefab, previewInstance.transform.position, Quaternion.identity);
                        placed.name = currentPrefab.name;
                        StartCoroutine(ConstructBuildingRoutine(placed, selectedBuildingData.buildTime));

                        // Terminar
                        CancelPlacing();

                        // NOTA: El GameManager ya revisa automáticamente en el Update.
                        // No hace falta llamarlo manualmente, pero no da error si lo dejas.
                    }
                    else
                    {
                        Debug.Log("No hay recursos suficientes");
                        GameEvents.RaiseInsufficientResources(); // Sonido
                    }
                }
                else
                {
                    GameEvents.RaiseInvalidCommand(); // Sonido de error
                }
            }
        }

        // --- 3. CANCELAR (CLIC DERECHO O ESCAPE) ---
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacing();
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        if (mainCam == null) mainCam = Camera.main;
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -mainCam.transform.position.z;
        Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePoint);
        worldPos.z = placeZ;
        return worldPos;
    }

    public void SelectBuilding(BuildingData buildingData)
    {
        if (buildingData == null) return;

        if (buildingData.buildingPrefab == null)
        {
            Debug.LogWarning($"BuildingManager: prefab não definido no BuildingData '{buildingData.buildingName}'.");
            return;
        }

        StartPlacing(buildingData);
        GameEvents.RaiseBuildingSelected(); // Feedback sonoro
    }

    public void StartPlacing(BuildingData data)
    {
        CancelPlacing(); // Limpia lo anterior

        selectedBuildingData = data;
        currentPrefab = data.buildingPrefab;
        isPlacing = true;

        // Crear el fantasma
        previewInstance = Instantiate(currentPrefab);
        previewInstance.name = currentPrefab.name + "_Preview";

        // Preparar visuales
        DisableRuntimeComponents(previewInstance);
        CacheRendererColors(previewInstance, previewOriginalColors);
        ApplyColorToPreview(previewInvalidColor);

        previewInstance.SetActive(true);

        Debug.Log($"BuildingManager: Modo construcción activado para '{data.buildingName}'.");
    }

    public void CancelPlacing()
    {
        if (previewInstance != null)
        {
            RestoreRendererColors(previewOriginalColors);
            Destroy(previewInstance);
        }

        previewInstance = null;
        selectedBuildingData = null;
        currentPrefab = null;
        isPlacing = false;

        foreach (var m in previewInstantiatedMaterials) if (m != null) Destroy(m);
        previewInstantiatedMaterials.Clear();
        previewOriginalColors.Clear();
    }

    public bool TrySpendResources(int amount)
    {
        if (amount <= 0) return true;
        if (!requireResources) return true;

        if (MoneyManager.Instance != null)
        {
            return MoneyManager.Instance.SpendMoney(amount);
        }
        else
        {
            Debug.LogWarning("[BuildingManager] MoneyManager não encontrado. Permitindo construção sem custo.");
            return true;
        }
    }

    IEnumerator ConstructBuildingRoutine(GameObject building, float timeToBuild)
    {
        // 1. Desactivamos componentes mientras se construye
        DisableRuntimeComponents(building);

        // Efecto visual de construcción
        Coroutine pulse = StartCoroutine(ConstructionPulseRoutine(building.transform));

        Debug.Log($"Construyendo {building.name}... Tiempo: {timeToBuild}s");

        // 2. Esperamos el tiempo necesario
        yield return new WaitForSeconds(timeToBuild);

        // 3. Limpieza y activación
        if (pulse != null) StopCoroutine(pulse);

        if (building != null)
        {
            building.transform.localScale = Vector3.one; // Resetear escala

            // Volvemos a activar los scripts del edificio real
            EnableRuntimeComponents(building);

            Debug.Log($"{building.name} completado!");
        }
    }

    IEnumerator ConstructionPulseRoutine(Transform target)
    {
        if (target == null) yield break;
        Vector3 baseScale = target.localScale;
        float elapsed = 0f;
        while (true)
        {
            if (target == null) yield break;

            elapsed += Time.deltaTime * constructionPulseSpeed;
            float s = 1f + Mathf.Sin(elapsed) * constructionPulseScale;
            target.localScale = baseScale * s;
            yield return null;
        }
    }

    // Validações principais
    bool IsValidPlacement(Vector3 worldPos, out Collider2D[] overlapping)
    {
        overlapping = null;

        // dentro do raio da base
        if (militaryBase != null)
        {
            float d = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(militaryBase.position.x, militaryBase.position.y));
            if (d > buildRadius) return false;
        }

        // Fog of War
        if (fogOfWar != null)
        {
            if (!fogOfWar.IsPositionVisitedOrVisible(worldPos))
            {
                return false;
            }
        }

        // overlap usando bounds do preview
        if (previewInstance != null)
        {
            Renderer rend = previewInstance.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Vector2 center = rend.bounds.center;
                Vector2 size = rend.bounds.size;
                if (size.sqrMagnitude < 0.0001f) size = Vector2.one * 0.5f;

                float angle = previewInstance.transform.eulerAngles.z;
                overlapping = Physics2D.OverlapBoxAll(center, size, angle, blockingLayers);
                if (overlapping != null && overlapping.Length > 0) return false;
                return true;
            }
        }

        overlapping = Physics2D.OverlapCircleAll(new Vector2(worldPos.x, worldPos.y), 0.5f, blockingLayers);
        return overlapping == null || overlapping.Length == 0;
    }

    void UpdatePreviewVisual(bool valid)
    {
        if (previewInstance == null) return;
        ApplyColorToPreview(valid ? previewValidColor : previewInvalidColor);
    }

    // Helpers: cache / restore / apply colors
    void CacheRendererColors(GameObject obj, Dictionary<Renderer, Color> dict)
    {
        dict.Clear();
        var spriteRends = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in spriteRends) dict[r] = r.color;

        var meshRends = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in meshRends)
        {
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                dict[r] = r.sharedMaterial.color;
        }
    }

    void RestoreRendererColors(Dictionary<Renderer, Color> dict)
    {
        foreach (var kv in dict)
        {
            var rend = kv.Key;
            if (rend == null) continue;

            if (rend is SpriteRenderer sr) sr.color = kv.Value;
            else
            {
                var mr = rend as Renderer;
                if (mr != null)
                {
                    for (int i = 0; i < mr.materials.Length; i++)
                    {
                        if (mr.materials[i] != null && mr.materials[i].HasProperty("_Color"))
                            mr.materials[i].color = kv.Value;
                    }
                }
            }
        }
    }

    void ApplyColorToPreview(Color target)
    {
        if (previewInstance == null) return;

        var spriteRends = previewInstance.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in spriteRends)
        {
            Color orig = previewOriginalColors.ContainsKey(r) ? previewOriginalColors[r] : r.color;
            float finalAlpha = orig.a * target.a;
            r.color = new Color(target.r, target.g, target.b, finalAlpha);
        }

        var meshRends = previewInstance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in meshRends)
        {
            Material[] mats = mr.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat != null && mat.HasProperty("_Color"))
                {
                    Material inst = new Material(mat);
                    Color orig = previewOriginalColors.ContainsKey(mr) ? previewOriginalColors[mr] : mat.color;
                    inst.color = new Color(target.r, target.g, target.b, orig.a * target.a);
                    mats[i] = inst;
                    previewInstantiatedMaterials.Add(inst);
                }
            }
            mr.materials = mats;
        }
    }

    void DisableRuntimeComponents(GameObject obj)
    {
        var cols = obj.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) c.enabled = false;

        var rbs = obj.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (var r in rbs) r.simulated = false;

        var behaviours = obj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == this) continue;
            b.enabled = false;
        }
    }

    void EnableRuntimeComponents(GameObject obj)
    {
        var cols = obj.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) c.enabled = true;

        var rbs = obj.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (var r in rbs) r.simulated = true;

        var behaviours = obj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == this) continue;
            b.enabled = true;
        }
    }

    Vector3 ApplyGridSnap(Vector3 pos, Vector2 grid)
    {
        if (grid.x <= 0f) grid.x = 1f;
        if (grid.y <= 0f) grid.y = 1f;
        float x = Mathf.Round(pos.x / grid.x) * grid.x;
        float y = Mathf.Round(pos.y / grid.y) * grid.y;
        return new Vector3(x, y, pos.z);
    }

    public bool IsPlacing() => isPlacing;
}