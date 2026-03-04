using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CameraInputAdapter : MonoBehaviour
{
    [Header("Referências de Actions (do asset Controls.inputactions)")]
    [Tooltip("Action para mover a câmara para cima (ex.: Gameplay/Camera Up).")]
    public InputActionReference cameraUpAction;

    [Tooltip("Action para mover a câmara para baixo (ex.: Gameplay/Camera Down).")]
    public InputActionReference cameraDownAction;

    [Tooltip("Action para mover a câmara para a esquerda (ex.: Gameplay/Camera Left).")]
    public InputActionReference cameraLeftAction;

    [Tooltip("Action para mover a câmara para a direita (ex.: Gameplay/Camera Right).")]
    public InputActionReference cameraRightAction;

    [Tooltip("Action de zoom da câmara (ex.: Gameplay/ZoomCamera, tipo Vector2 ou Axis).")]
    public InputActionReference zoomCameraAction;

    [Header("Lógica de câmara")]
    public cameraFollow cameraFollow;

    void OnEnable()
    {
        EnableAction(cameraUpAction);
        EnableAction(cameraDownAction);
        EnableAction(cameraLeftAction);
        EnableAction(cameraRightAction);
        EnableAction(zoomCameraAction);
    }

    void OnDisable()
    {
        DisableAction(cameraUpAction);
        DisableAction(cameraDownAction);
        DisableAction(cameraLeftAction);
        DisableAction(cameraRightAction);
        DisableAction(zoomCameraAction);
    }

    void Update()
    {
        if (cameraFollow == null)
            return;

        // Construir Vector2 a partir de 4 actions de botão (float)
        Vector2 move = Vector2.zero;

        if (GetBool(cameraUpAction))
            move.y += 1f;
        if (GetBool(cameraDownAction))
            move.y -= 1f;
        if (GetBool(cameraLeftAction))
            move.x -= 1f;
        if (GetBool(cameraRightAction))
            move.x += 1f;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        if (move.sqrMagnitude > 0.0001f)
            cameraFollow.ManualMove(move);

        // Zoom
        if (zoomCameraAction != null && zoomCameraAction.action != null)
        {
            // Se a action for Vector2 (Mouse/scroll), lê y
            // Se for Axis, lê float
            float zoomDelta = 0f;

            var action = zoomCameraAction.action;
            var valueType = action.expectedControlType;

            if (valueType == "Vector2")
            {
                Vector2 scroll = action.ReadValue<Vector2>();
                zoomDelta = scroll.y;
            }
            else
            {
                zoomDelta = action.ReadValue<float>();
            }

            if (Mathf.Abs(zoomDelta) > 0.0001f)
                cameraFollow.ManualZoom(zoomDelta);
        }
    }

    private void EnableAction(InputActionReference reference)
    {
        if (reference != null && reference.action != null)
            reference.action.Enable();
    }

    private void DisableAction(InputActionReference reference)
    {
        if (reference != null && reference.action != null)
            reference.action.Disable();
    }

    private bool GetBool(InputActionReference reference)
    {
        if (reference == null || reference.action == null)
            return false;

        // Para actions de botão, ReadValue<float>() devolve 0 ou 1
        float v = reference.action.ReadValue<float>();
        return v > 0.5f;
    }
}