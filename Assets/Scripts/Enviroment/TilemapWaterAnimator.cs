using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Alterna entre dois sprites em um Tilemap para animar água (simples ondulação).
/// - Anexe ao GameObject que contém o Tilemap (ou atribua o Tilemap no Inspector).
/// - Pode filtrar pelas tiles originais (waterTileReference) ou detectar por sprite.
/// </summary>
[DisallowMultipleComponent]
public class TilemapWaterAnimator : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap; // se vazio, tenta GetComponent<Tilemap>()

    [Header("Detecção de água")]
    [Tooltip("Opcional: TileBase de referência para identificar tiles de água. Se vazio, detecta por spriteA/spriteB.")]
    public TileBase waterTileReference;

    [Header("Sprites (frames)")]
    public Sprite spriteA;
    public Sprite spriteB;

    [Header("Timing")]
    [Tooltip("Intervalo entre trocas (segundos) — ciclo completo = interval * 2")]
    public float interval = 0.5f;

    [Header("Desincronizar")]
    [Tooltip("Se true, metade das células alternam em fase oposta (efeito mais natural)")]
    public bool randomPhase = true;

    // runtime
    private List<Vector3Int> waterCells = new List<Vector3Int>();
    private List<bool> invertPhase = new List<bool>();
    private Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();
    private Tile tileA;
    private Tile tileB;
    private Coroutine animRoutine;

    void Awake()
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
    }

    void OnEnable()
    {
        if (tilemap == null)
        {
            Debug.LogWarning("TilemapWaterAnimator: Tilemap não atribuído e não encontrado no GameObject.");
            return;
        }

        if (spriteA == null || spriteB == null)
        {
            Debug.LogWarning("TilemapWaterAnimator: sprites não atribuídos. Cancela animação.");
            return;
        }

        BuildTiles();
        CollectWaterCells();
        StartAnimation();
    }

    void OnDisable()
    {
        StopAnimation();
        RestoreOriginalTiles();
        DestroyTiles();
    }

    void BuildTiles()
    {
        tileA = ScriptableObject.CreateInstance<Tile>();
        tileA.sprite = spriteA;
        tileA.color = Color.white;

        tileB = ScriptableObject.CreateInstance<Tile>();
        tileB.sprite = spriteB;
        tileB.color = Color.white;
    }

    void DestroyTiles()
    {
        if (tileA != null) Destroy(tileA);
        if (tileB != null) Destroy(tileB);
        tileA = null;
        tileB = null;
    }

    void CollectWaterCells()
    {
        waterCells.Clear();
        invertPhase.Clear();
        originalTiles.Clear();

        BoundsInt bounds = tilemap.cellBounds;
        // Percorre todas as células na bounding box do Tilemap
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                TileBase t = tilemap.GetTile(cell);
                if (t == null) continue;

                bool isWater = false;
                if (waterTileReference != null)
                {
                    if (t == waterTileReference) isWater = true;
                }
                else
                {
                    // detecta por sprite (se tile for Tile)
                    Tile tile = t as Tile;
                    if (tile != null && (tile.sprite == spriteA || tile.sprite == spriteB))
                        isWater = true;
                }

                if (isWater)
                {
                    waterCells.Add(cell);
                    originalTiles[cell] = t;
                    invertPhase.Add(randomPhase && (Random.value > 0.5f));
                }
            }
        }

        if (waterCells.Count == 0)
            Debug.LogWarning("TilemapWaterAnimator: não encontrou células de água com as condições definidas.");
    }

    void StartAnimation()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateTilesCoroutine());
    }

    void StopAnimation()
    {
        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }
    }

    IEnumerator AnimateTilesCoroutine()
    {
        bool state = false;
        while (true)
        {
            // alterna estado
            state = !state;

            for (int i = 0; i < waterCells.Count; i++)
            {
                Vector3Int cell = waterCells[i];
                bool invert = invertPhase[i];
                bool showA = (state ^ invert); // se true -> spriteA, else spriteB
                tilemap.SetTile(cell, showA ? tileA : tileB);
            }

            yield return new WaitForSeconds(interval);
        }
    }

    void RestoreOriginalTiles()
    {
        foreach (var kv in originalTiles)
        {
            tilemap.SetTile(kv.Key, kv.Value);
        }
        originalTiles.Clear();
    }

    // API pública
    public void StartRipples()
    {
        if (animRoutine == null) StartAnimation();
    }

    public void StopRipples()
    {
        StopAnimation();
        RestoreOriginalTiles();
    }

    // Permite atualizar sprites em runtime
    public void SetSprites(Sprite a, Sprite b)
    {
        spriteA = a;
        spriteB = b;
        if (tileA != null) tileA.sprite = spriteA;
        if (tileB != null) tileB.sprite = spriteB;
    }

    // Re-coleta células (use se modificares o tilemap em runtime)
    public void Refresh()
    {
        StopAnimation();
        RestoreOriginalTiles();
        CollectWaterCells();
        StartAnimation();
    }
}