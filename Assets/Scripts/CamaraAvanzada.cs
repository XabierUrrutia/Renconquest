using UnityEngine;

public class CamaraAvanzada : MonoBehaviour
{
    [Header("Movimiento Básico")]
    public float velocidadNormal = 5f;
    public float velocidadRapida = 10f;

    [Header("Movimiento con Ratón")]
    public bool movimientoConRatón = true;
    public float bordePantalla = 10f;
    public float velocidadRatón = 5f;

    [Header("Límites - Y entre -25 y 25")]
    public float limiteInferior = -25f;
    public float limiteSuperior = 25f;
    public float limiteIzquierdo = -50f;
    public float limiteDerecho = 50f;

    private Vector3 posicionObjetivo;
    private float velocidadActual;

    void Start()
    {
        posicionObjetivo = transform.position;
        // Aplicar límites inmediatamente
        posicionObjetivo = AplicarLimites(posicionObjetivo);
        transform.position = posicionObjetivo;
    }

    void Update()
    {
        CalcularVelocidad();
        ProcesarTeclado();
        if (movimientoConRatón) ProcesarRatón();
        AplicarMovimiento();
    }

    void CalcularVelocidad()
    {
        velocidadActual = Input.GetKey(KeyCode.LeftShift) ? velocidadRapida : velocidadNormal;
    }

    void ProcesarTeclado()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
        posicionObjetivo += input * velocidadActual * Time.deltaTime;
        posicionObjetivo = AplicarLimites(posicionObjetivo);
    }

    void ProcesarRatón()
    {
        Vector3 movimientoRatón = Vector3.zero;
        Vector3 posicionRatón = Input.mousePosition;

        if (posicionRatón.y <= bordePantalla)
            movimientoRatón.y -= 1;
        else if (posicionRatón.y >= Screen.height - bordePantalla)
            movimientoRatón.y += 1;

        posicionObjetivo += movimientoRatón * velocidadRatón * Time.deltaTime;
        posicionObjetivo = AplicarLimites(posicionObjetivo);
    }

    Vector3 AplicarLimites(Vector3 posicion)
    {
        return new Vector3(
            Mathf.Clamp(posicion.x, limiteIzquierdo, limiteDerecho),
            Mathf.Clamp(posicion.y, limiteInferior, limiteSuperior),
            posicion.z
        );
    }

    void AplicarMovimiento()
    {
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, 5f * Time.deltaTime);
    }

    // Debug para verificar límites en el build
    void OnGUI()
    {
        if (Input.GetKey(KeyCode.F1))
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Posición: {transform.position}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Límites Y: {limiteInferior} a {limiteSuperior}");
        }
    }
}