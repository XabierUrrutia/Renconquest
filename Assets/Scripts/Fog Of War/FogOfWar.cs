using UnityEngine;
using System.Collections.Generic;

public class FogOfWar : MonoBehaviour
{
    [Header("Configuración del Mapa")]
    public string playerTag = "Player";
    public float defaultVisionRadius = 5f;
    public Vector2 isometricMapSize = new Vector2(300f, 200f);

    [Header("Posición del Mapa en el Mundo")]
    public Vector2 mapWorldPosition = new Vector2(-1575f, -240f);

    [Header("Renderers")]
    public SpriteRenderer blackFogRenderer;
    public SpriteRenderer visionRenderer;
    public SpriteRenderer visitedRenderer;

    [Header("Texturas")]
    public int textureSize = 1024;

    [Header("Configuración Áreas Visitadas")]
    public Color visitedColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

    private List<FogPlayer> fogPlayers = new List<FogPlayer>();
    private List<FogStaticVision> staticVisions = new List<FogStaticVision>();
    private Texture2D blackFogTexture;
    private Texture2D visionTexture;
    private Texture2D visitedTexture;
    private Color32[] blackFogPixels;
    private Color32[] visionPixels;
    private Color32[] visitedPixels;
    private bool[] revealedPixels;
    private bool[] currentVisionPixels;
    private bool[] visitedFlags;

    private bool needsUpdate = false;
    private Bounds worldBounds;
    private float pixelsPerUnitX;
    private float pixelsPerUnitY;

    void Start()
    {
        CalculateWorldBounds();
        CalculatePixelsPerUnit();
        InitializeTextures();
        PositionFogRenderers();
        SetupMaterials();
        FindAllPlayers();
    }

    void LateUpdate()
    {
        if (needsUpdate)
        {
            UpdateFogOfWar();
            needsUpdate = false;
        }
    }

    void CalculateWorldBounds()
    {
        Vector3 center = new Vector3(
            mapWorldPosition.x + isometricMapSize.x * 0.5f,
            mapWorldPosition.y + isometricMapSize.y * 0.5f,
            0f
        );

        Vector3 size = new Vector3(isometricMapSize.x, isometricMapSize.y, 0f);
        worldBounds = new Bounds(center, size);
    }

    void CalculatePixelsPerUnit()
    {
        pixelsPerUnitX = textureSize / isometricMapSize.x;
        pixelsPerUnitY = textureSize / isometricMapSize.y;
    }

    void SetupMaterials()
    {
        // Configura materiales para blending adecuado
        if (visionRenderer != null)
        {
            visionRenderer.material = new Material(Shader.Find("Sprites/Default"));
            visionRenderer.color = Color.white;
        }

        if (blackFogRenderer != null)
        {
            blackFogRenderer.material = new Material(Shader.Find("Sprites/Default"));
            blackFogRenderer.color = Color.white;
        }

        if (visitedRenderer != null)
        {
            visitedRenderer.material = new Material(Shader.Find("Sprites/Default"));
            visitedRenderer.color = Color.white;
        }
    }

    void FindAllPlayers()
    {
        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (GameObject playerObj in playerObjs)
        {
            FogPlayer fogPlayer = playerObj.GetComponent<FogPlayer>();
            if (fogPlayer != null && !fogPlayers.Contains(fogPlayer))
            {
                fogPlayers.Add(fogPlayer);
                fogPlayer.SetFogOfWar(this);
            }
        }

        if (fogPlayers.Count > 0)
        {
            needsUpdate = true;
        }
    }

    public void RegisterPlayer(FogPlayer fogPlayer)
    {
        if (!fogPlayers.Contains(fogPlayer))
        {
            fogPlayers.Add(fogPlayer);
            needsUpdate = true;
        }
    }

    public void UnregisterPlayer(FogPlayer fogPlayer)
    {
        if (fogPlayers.Contains(fogPlayer))
        {
            fogPlayers.Remove(fogPlayer);
            needsUpdate = true;
        }
    }

    public void RequestUpdate()
    {
        needsUpdate = true;
    }

    void InitializeTextures()
    {
        blackFogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        visionTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        visitedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        blackFogTexture.wrapMode = TextureWrapMode.Clamp;
        visionTexture.wrapMode = TextureWrapMode.Clamp;
        visitedTexture.wrapMode = TextureWrapMode.Clamp;

        blackFogTexture.filterMode = FilterMode.Point;
        visionTexture.filterMode = FilterMode.Point;
        visitedTexture.filterMode = FilterMode.Point;

        int totalPixels = textureSize * textureSize;
        blackFogPixels = new Color32[totalPixels];
        visionPixels = new Color32[totalPixels];
        visitedPixels = new Color32[totalPixels];
        revealedPixels = new bool[totalPixels];
        currentVisionPixels = new bool[totalPixels];
        visitedFlags = new bool[totalPixels];

        Color32 black = new Color32(0, 0, 0, 255);
        Color32 transparent = new Color32(0, 0, 0, 0);

        for (int i = 0; i < totalPixels; i++)
        {
            blackFogPixels[i] = black;
            visionPixels[i] = transparent;
            visitedPixels[i] = transparent;
            revealedPixels[i] = false;
            currentVisionPixels[i] = false;
            visitedFlags[i] = false;
        }

        ApplyTextures();
        CreateSprites();
    }

    void UpdateFogOfWar()
    {
        if (fogPlayers.Count == 0 && staticVisions.Count == 0) return;

        ClearVisionTexture();

        // Procesar visión de jugadores
        foreach (FogPlayer fogPlayer in fogPlayers)
        {
            if (fogPlayer != null && fogPlayer.gameObject.activeInHierarchy)
            {
                Vector3 playerWorldPos = fogPlayer.transform.position;
                Vector2Int playerPixel = WorldToPixel(playerWorldPos);

                float avgPixelsPerUnit = (pixelsPerUnitX + pixelsPerUnitY) * 0.5f;
                int pixelRadius = Mathf.RoundToInt(fogPlayer.visionRadius * avgPixelsPerUnit);
                pixelRadius = Mathf.Max(1, pixelRadius);

                DrawVisionCircle(playerPixel, pixelRadius);
                UpdateVisitedAreas(playerPixel, pixelRadius);
            }
        }

        // Procesar visión estática de bases
        foreach (FogStaticVision staticVision in staticVisions)
        {
            if (staticVision != null && staticVision.gameObject.activeInHierarchy)
            {
                Vector3 staticWorldPos = staticVision.GetPosition();
                Vector2Int staticPixel = WorldToPixel(staticWorldPos);

                float avgPixelsPerUnit = (pixelsPerUnitX + pixelsPerUnitY) * 0.5f;
                int pixelRadius = Mathf.RoundToInt(staticVision.visionRadius * avgPixelsPerUnit);
                pixelRadius = Mathf.Max(1, pixelRadius);

                DrawVisionCircle(staticPixel, pixelRadius);
                UpdateVisitedAreas(staticPixel, pixelRadius);
            }
        }

        ApplyTextures();
        DebugTextureStates();
    }

    Vector2Int WorldToPixel(Vector3 worldPos)
    {
        float relativeX = (worldPos.x - mapWorldPosition.x) / isometricMapSize.x;
        float relativeY = (worldPos.y - mapWorldPosition.y) / isometricMapSize.y;

        relativeX = Mathf.Clamp01(relativeX);
        relativeY = Mathf.Clamp01(relativeY);

        int pixelX = Mathf.FloorToInt(relativeX * (textureSize - 1));
        int pixelY = Mathf.FloorToInt(relativeY * (textureSize - 1));

        return new Vector2Int(pixelX, pixelY);
    }

    void ClearVisionTexture()
    {
        Color32 transparent = new Color32(0, 0, 0, 0);
        for (int i = 0; i < visionPixels.Length; i++)
        {
            visionPixels[i] = transparent;
            currentVisionPixels[i] = false;
        }
    }

    void DrawVisionCircle(Vector2Int center, int radius)
    {
        int radiusSqr = radius * radius;
        Color32 transparent = new Color32(0, 0, 0, 0);

        int startX = Mathf.Max(0, center.x - radius);
        int endX = Mathf.Min(textureSize - 1, center.x + radius);
        int startY = Mathf.Max(0, center.y - radius);
        int endY = Mathf.Min(textureSize - 1, center.y + radius);

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                int dx = x - center.x;
                int dy = y - center.y;
                int distSqr = dx * dx + dy * dy;

                if (distSqr <= radiusSqr)
                {
                    int index = y * textureSize + x;
                    visionPixels[index] = transparent;
                    currentVisionPixels[index] = true;

                    // Marcar como revelado permanentemente
                    if (!revealedPixels[index])
                    {
                        revealedPixels[index] = true;
                        blackFogPixels[index] = new Color32(0, 0, 0, 150); // Semitransparente
                    }
                }
            }
        }
    }

    void UpdateVisitedAreas(Vector2Int center, int radius)
    {
        int radiusSqr = radius * radius;
        Color32 visitedColor32 = new Color32(
            (byte)(visitedColor.r * 255),
            (byte)(visitedColor.g * 255),
            (byte)(visitedColor.b * 255),
            (byte)(visitedColor.a * 255)
        );

        int startX = Mathf.Max(0, center.x - radius);
        int endX = Mathf.Min(textureSize - 1, center.x + radius);
        int startY = Mathf.Max(0, center.y - radius);
        int endY = Mathf.Min(textureSize - 1, center.y + radius);

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                int dx = x - center.x;
                int dy = y - center.y;

                if (dx * dx + dy * dy <= radiusSqr)
                {
                    int index = y * textureSize + x;
                    if (!visitedFlags[index])
                    {
                        visitedFlags[index] = true;
                        visitedPixels[index] = visitedColor32;
                    }
                }
            }
        }
    }

    void ApplyTextures()
    {
        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32 visitedColor32 = new Color32(
            (byte)(visitedColor.r * 255),
            (byte)(visitedColor.g * 255),
            (byte)(visitedColor.b * 255),
            (byte)(visitedColor.a * 255)
        );

        for (int i = 0; i < visitedPixels.Length; i++)
        {
            if (currentVisionPixels[i])
            {
                visitedPixels[i] = transparent;
                blackFogPixels[i] = transparent;
            }
            else if (visitedFlags[i])
            {
                visitedPixels[i] = visitedColor32;
                blackFogPixels[i] = new Color32(0, 0, 0, 150);
            }
            else
            {
                blackFogPixels[i] = new Color32(0, 0, 0, 255);
                visitedPixels[i] = transparent;
            }
        }

        blackFogTexture.SetPixels32(blackFogPixels);
        visitedTexture.SetPixels32(visitedPixels);
        visionTexture.SetPixels32(visionPixels);

        blackFogTexture.Apply();
        visitedTexture.Apply();
        visionTexture.Apply();
    }

    void CreateSprites()
    {
        if (visitedRenderer != null)
        {
            Rect rect = new Rect(0, 0, textureSize, textureSize);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            float pixelsPerUnit = 100f;
            visitedRenderer.sprite = Sprite.Create(visitedTexture, rect, pivot, pixelsPerUnit);
            visitedRenderer.sortingOrder = 5;
        }

        if (blackFogRenderer != null)
        {
            Rect rect = new Rect(0, 0, textureSize, textureSize);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            float pixelsPerUnit = 100f;
            blackFogRenderer.sprite = Sprite.Create(blackFogTexture, rect, pivot, pixelsPerUnit);
            blackFogRenderer.sortingOrder = 6;
        }

        if (visionRenderer != null)
        {
            Rect rect = new Rect(0, 0, textureSize, textureSize);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            float pixelsPerUnit = 100f;
            visionRenderer.sprite = Sprite.Create(visionTexture, rect, pivot, pixelsPerUnit);
            visionRenderer.sortingOrder = 7;
        }
    }

    void PositionFogRenderers()
    {
        Vector3 rendererPosition = new Vector3(
            mapWorldPosition.x + isometricMapSize.x * 0.5f,
            mapWorldPosition.y + isometricMapSize.y * 0.5f,
            0f
        );

        if (visitedRenderer != null)
        {
            visitedRenderer.transform.position = rendererPosition;
            float scaleX = isometricMapSize.x / (textureSize / 100f);
            float scaleY = isometricMapSize.y / (textureSize / 100f);
            visitedRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        if (blackFogRenderer != null)
        {
            blackFogRenderer.transform.position = rendererPosition;
            float scaleX = isometricMapSize.x / (textureSize / 100f);
            float scaleY = isometricMapSize.y / (textureSize / 100f);
            blackFogRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        if (visionRenderer != null)
        {
            visionRenderer.transform.position = rendererPosition;
            float scaleX = isometricMapSize.x / (textureSize / 100f);
            float scaleY = isometricMapSize.y / (textureSize / 100f);
            visionRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }

    void DebugTextureStates()
    {
        int visiblePixels = 0;
        int visitedPixelsCount = 0;
        int revealedPixelsCount = 0;

        for (int i = 0; i < currentVisionPixels.Length; i++)
        {
            if (currentVisionPixels[i]) visiblePixels++;
            if (visitedFlags[i]) visitedPixelsCount++;
            if (revealedPixels[i]) revealedPixelsCount++;
        }
    }

    public void SetVisitedColor(Color newColor)
    {
        visitedColor = newColor;
        needsUpdate = true;
    }

    public void DebugPosition(Vector3 worldPosition, string objectName = "")
    {
        Vector2Int pixel = WorldToPixel(worldPosition);
    }

    // Métodos públicos de consulta (único, sem duplicação)
    public bool IsPositionVisible(Vector3 worldPosition)
    {
        Vector2Int pixel = WorldToPixel(worldPosition);
        int index = pixel.y * textureSize + pixel.x;
        if (index < 0 || index >= currentVisionPixels.Length) return false;
        return currentVisionPixels[index];
    }

    public bool IsPositionRevealed(Vector3 worldPosition)
    {
        Vector2Int pixel = WorldToPixel(worldPosition);
        int index = pixel.y * textureSize + pixel.x;
        if (index < 0 || index >= revealedPixels.Length) return false;
        return revealedPixels[index];
    }

    public bool IsPositionVisited(Vector3 worldPosition)
    {
        Vector2Int pixel = WorldToPixel(worldPosition);
        int index = pixel.y * textureSize + pixel.x;
        if (index < 0 || index >= visitedFlags.Length) return false;
        return visitedFlags[index];
    }

    // Combinado útil para validação de construção
    public bool IsPositionVisitedOrVisible(Vector3 worldPos)
    {
        Vector2Int px = WorldToPixel(worldPos);
        int index = px.y * textureSize + px.x;
        if (index < 0 || index >= revealedPixels.Length) return false;
        return revealedPixels[index] || currentVisionPixels[index] || visitedFlags[index];
    }

    // Expor WorldToPixel se necessário (útil para debug)
    public Vector2Int GetPixelForWorldPosition(Vector3 worldPos)
    {
        return WorldToPixel(worldPos);
    }

    void OnDestroy()
    {
        if (blackFogTexture != null) Destroy(blackFogTexture);
        if (visionTexture != null) Destroy(visionTexture);
        if (visitedTexture != null) Destroy(visitedTexture);
        fogPlayers.Clear();
        staticVisions.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

        Gizmos.color = Color.green;
        foreach (FogPlayer player in fogPlayers)
        {
            if (player != null)
            {
                Gizmos.DrawSphere(player.transform.position, 0.5f);
                Gizmos.DrawWireSphere(player.transform.position, player.visionRadius);
            }
        }
    }

    public void RegisterStaticVision(FogStaticVision staticVision)
    {
        if (!staticVisions.Contains(staticVision))
        {
            staticVisions.Add(staticVision);
            needsUpdate = true;
        }
    }

    public void UnregisterStaticVision(FogStaticVision staticVision)
    {
        if (staticVisions.Contains(staticVision))
        {
            staticVisions.Remove(staticVision);
            needsUpdate = true;
        }
    }
}