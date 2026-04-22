using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class TilemapWaterAnimator : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;

    [Header("Detecção de água")]
    public TileBase waterTileReference;

    [Header("Sprites (frames)")]
    public Sprite spriteA;
    public Sprite spriteB;

    [Header("Timing")]
    public float interval = 0.5f;

    private Vector3Int[] waterCells;
    private TileBase[] framesA;
    private TileBase[] framesB;
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
        if (tilemap == null || spriteA == null || spriteB == null)
        {
            Debug.LogWarning("TilemapWaterAnimator: faltan referencias.");
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
        originalTiles.Clear();
        List<Vector3Int> found = new List<Vector3Int>();

        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
        int width = bounds.size.x;

        for (int y = 0; y < bounds.size.y; y++)
        {
            for (int x = 0; x < bounds.size.x; x++)
            {
                TileBase t = allTiles[x + y * width];
                if (t == null) continue;

                bool isWater = false;
                if (waterTileReference != null) isWater = (t == waterTileReference);
                else
                {
                    Tile tile = t as Tile;
                    if (tile != null && (tile.sprite == spriteA || tile.sprite == spriteB))
                        isWater = true;
                }

                if (isWater)
                {
                    Vector3Int cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                    found.Add(cell);
                    originalTiles[cell] = t;
                }
            }
        }

        waterCells = found.ToArray();
        framesA = new TileBase[waterCells.Length];
        framesB = new TileBase[waterCells.Length];
        for (int i = 0; i < waterCells.Length; i++)
        {
            framesA[i] = tileA;
            framesB[i] = tileB;
        }

        if (waterCells.Length == 0)
            Debug.LogWarning("TilemapWaterAnimator: sin celdas de agua.");
    }

    void StartAnimation()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateTilesCoroutine());
    }

    void StopAnimation()
    {
        if (animRoutine != null) { StopCoroutine(animRoutine); animRoutine = null; }
    }

    IEnumerator AnimateTilesCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(interval);
        bool state = false;
        while (true)
        {
            state = !state;
            tilemap.SetTiles(waterCells, state ? framesA : framesB);
            yield return wait;
        }
    }

    void RestoreOriginalTiles()
    {
        foreach (var kv in originalTiles) tilemap.SetTile(kv.Key, kv.Value);
        originalTiles.Clear();
    }

    public void StartRipples() { if (animRoutine == null) StartAnimation(); }
    public void StopRipples() { StopAnimation(); RestoreOriginalTiles(); }

    public void Refresh()
    {
        StopAnimation();
        RestoreOriginalTiles();
        CollectWaterCells();
        StartAnimation();
    }
}