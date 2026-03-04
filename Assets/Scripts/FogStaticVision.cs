using UnityEngine;

public class FogStaticVision : MonoBehaviour
{
    [Header("Static Vision Settings")]
    public float visionRadius = 20f;
    public bool alwaysActive = true;

    private FogOfWar fogOfWar;
    public bool isInitialized = false;

    void Start()
    {
        InitializeFogSystem();
    }

    void Update()
    {
        // Visión estática - no necesita actualizarse cada frame como el jugador
        // pero forzamos una actualización periódica por si acaso
        if (isInitialized && alwaysActive && fogOfWar != null)
        {
            // Actualizar cada 2 segundos para asegurar que la visión se mantiene
            if (Time.frameCount % 120 == 0) // Aprox cada 2 segundos a 60 FPS
            {
                fogOfWar.RequestUpdate();
            }
        }
    }

    private void InitializeFogSystem()
    {
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();

        if (fogOfWar == null)
        {
            Invoke("InitializeFogSystem", 0.1f);
            return;
        }

        // Registrar esta visión estática en el sistema de niebla
        fogOfWar.RegisterStaticVision(this);
        isInitialized = true;

        // Forzar primera actualización
        fogOfWar.RequestUpdate();
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    void OnDestroy()
    {
        if (fogOfWar != null && isInitialized)
        {
            fogOfWar.UnregisterStaticVision(this);
        }
    }

    void OnEnable()
    {
        if (fogOfWar != null && !isInitialized)
        {
            fogOfWar.RegisterStaticVision(this);
            isInitialized = true;
            fogOfWar.RequestUpdate();
        }
    }

    void OnDisable()
    {
        if (fogOfWar != null && isInitialized)
        {
            fogOfWar.UnregisterStaticVision(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }
}