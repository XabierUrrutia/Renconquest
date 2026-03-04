using UnityEngine;

public class cameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Movimiento Libre con Teclado")]
    public float moveSpeed = 5f;
    private bool isManualControl = true;

    [Header("Límites de la Cámara")]
    public bool useBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;

    [Header("Zoom (Mouse Scroll)")]
    [Tooltip("Camera a controlar. Se vazio, tentará obter Camera.main ou Camera no mesmo GameObject.")]
    public Camera cam;
    public float zoomSpeed = 5f;
    public float zoomSmoothSpeed = 10f;
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 10f;
    public float minFOV = 15f;
    public float maxFOV = 60f;

    private float _targetZoom; // orthoSize ou FOV dependendo do tipo
    private bool _isOrthographic = true;

    // >>> NOVO: índice de nível de zoom (0 = mais perto)
    [Header("Níveis de zoom discretos")]
    public float[] orthoZoomLevels = new float[4] { 3f, 5f, 7f, 10f };
    public float[] fovZoomLevels = new float[4] { 25f, 35f, 45f, 60f };
    private int _currentZoomLevel = 1;

    void Start()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                cam = Camera.main;
        }

        if (cam != null)
        {
            _isOrthographic = cam.orthographic;
            _targetZoom = _isOrthographic ? cam.orthographicSize : cam.fieldOfView;

            // Clamp níveis dentro dos limites
            if (orthoZoomLevels == null || orthoZoomLevels.Length == 0)
                orthoZoomLevels = new float[4] { 3f, 5f, 7f, 10f };

            if (fovZoomLevels == null || fovZoomLevels.Length == 0)
                fovZoomLevels = new float[4] { 25f, 35f, 45f, 60f };

            for (var i = 0; i < orthoZoomLevels.Length; i++)
                orthoZoomLevels[i] = Mathf.Clamp(orthoZoomLevels[i], minOrthoSize, maxOrthoSize);

            for (var i = 0; i < fovZoomLevels.Length; i++)
                fovZoomLevels[i] = Mathf.Clamp(fovZoomLevels[i], minFOV, maxFOV);

            _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, orthoZoomLevels.Length - 1);

            if (_isOrthographic)
            {
                _targetZoom = orthoZoomLevels[_currentZoomLevel];
                cam.orthographicSize = _targetZoom;
            }
            else
            {
                _targetZoom = fovZoomLevels[_currentZoomLevel];
                cam.fieldOfView = _targetZoom;
            }
        }
    }

    void LateUpdate()
    {
        HandleZoomInput();

        if (!isManualControl && target != null)
        {
            Vector3 desiredPosition = target.position + offset;

            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }

        if (cam != null)
        {
            if (_isOrthographic)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, _targetZoom, Time.deltaTime * zoomSmoothSpeed);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _targetZoom, Time.deltaTime * zoomSmoothSpeed);
            }
        }
    }

    void HandleZoomInput()
    {
        if (cam == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            // scroll > 0 → zoom in (mais perto) → diminuir nível
            if (scroll > 0f)
                _currentZoomLevel--;
            else
                _currentZoomLevel++;

            if (_isOrthographic)
            {
                _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, orthoZoomLevels.Length - 1);
                _targetZoom = orthoZoomLevels[_currentZoomLevel];
            }
            else
            {
                _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, fovZoomLevels.Length - 1);
                _targetZoom = fovZoomLevels[_currentZoomLevel];
            }
        }
    }

    public void ManualMove(Vector2 input)
    {
        isManualControl = true;

        Vector3 moveInput = new Vector3(input.x, input.y, 0f);
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 desiredPosition = transform.position + moveInput * moveSpeed * Time.deltaTime;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        transform.position = desiredPosition;
    }

    public void ManualZoom(float scrollDelta)
    {
        if (cam == null)
            return;

        if (Mathf.Abs(scrollDelta) <= 0.0001f)
            return;

        if (scrollDelta > 0f)
            _currentZoomLevel--;
        else
            _currentZoomLevel++;

        if (_isOrthographic)
        {
            _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, orthoZoomLevels.Length - 1);
            _targetZoom = orthoZoomLevels[_currentZoomLevel];
        }
        else
        {
            _currentZoomLevel = Mathf.Clamp(_currentZoomLevel, 0, fovZoomLevels.Length - 1);
            _targetZoom = fovZoomLevels[_currentZoomLevel];
        }
    }

    // <<< NOVO: indica se está no zoom mais perto (nível 0)
    public bool IsAtClosestZoom()
    {
        return _currentZoomLevel == 0;
    }

    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}