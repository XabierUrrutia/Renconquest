using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapInteraction : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Referencias")]
    [Tooltip("La cámara que está renderizando el Minimapa (NO la Main Camera)")]
    public Camera minimapCamera;

    [Tooltip("La cámara principal del juego (la que se mueve)")]
    public Camera mainCamera;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    // Al hacer click
    public void OnPointerDown(PointerEventData eventData)
    {
        MoverCamara(eventData);
    }

    // Al arrastrar el ratón por el minimapa (opcional, para moverte rápido)
    public void OnDrag(PointerEventData eventData)
    {
        MoverCamara(eventData);
    }

    void MoverCamara(PointerEventData eventData)
    {
        if (minimapCamera == null) return;

        // 1. Convertir click de pantalla a posición local en la RawImage
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // 2. Normalizar coordenadas (0 a 1)
            // Esto nos dice: "Has clickado en el 50% ancho y 50% alto de la imagen"
            float rectWidth = rectTransform.rect.width;
            float rectHeight = rectTransform.rect.height;

            // Ajuste porque el pivote suele estar en el centro (0,0) en UI
            // localPoint va de -width/2 a +width/2. Lo pasamos a 0..1
            float viewportX = (localPoint.x / rectWidth) + 0.5f;
            float viewportY = (localPoint.y / rectHeight) + 0.5f;

            // 3. LA MAGIA: Preguntar a la cámara del minimapa dónde es eso en el mundo
            // Z=0 porque en 2D/Isométrico trabajamos en el plano
            Vector3 worldPos = minimapCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, minimapCamera.nearClipPlane));

            // 4. Mover la cámara principal
            // Mantenemos la Z original de la MainCamera para no "enterrarla"
            Vector3 targetPos = new Vector3(worldPos.x, worldPos.y, mainCamera.transform.position.z);

            mainCamera.transform.position = targetPos;
        }
    }
}