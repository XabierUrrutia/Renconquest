using UnityEngine;
public class HealthBarBillboard : MonoBehaviour
{
    private Camera cam;
    void Start()
    {
        cam = Camera.main;
    }
    void LateUpdate()
    {
        // Mantener la barra siempre orientada hacia la cámara (para perspectiva isométrica)
        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }
    }
}