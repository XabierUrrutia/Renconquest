using UnityEngine;
using System.Collections;

public class TankVisuals : MonoBehaviour
{
    [Header("Referencias")]
    public SpriteRenderer spriteRenderer;

    [Header("Configuración")]
    public float velocidadAnimacion = 0.2f;
    public float velocidadDisparo = 0.1f;

    [System.Serializable]
    public struct SpritesDireccion
    {
        public string nombre;
        public Sprite[] moveSprites;  // 2 frames andar
        public Sprite[] shootSprites; // 3 frames disparo
    }

    [Header("Orden: 0:NE, 1:SE, 2:SW, 3:NW")]
    public SpritesDireccion[] direcciones;

    // Estado interno
    private int indiceDireccion = 1;
    private int frameMovimiento = 0;
    private float timerAnim;
    private bool isShooting = false;
    private Coroutine shootRoutine;

    // --- LÓGICA PRINCIPAL ---

    // Llamado por TankMovement cada frame
    public void ActualizarVisuales(Vector2 direccion, bool seMueve)
    {
        if (isShooting) return;

        // 1. CALCULAR GIRO
        if (direccion != Vector2.zero)
        {
            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            if (angulo < 0) angulo += 360;

            indiceDireccion = ObtenerIndicePorAngulo(angulo);
        }

        // 2. ANIMACIÓN (Bote)
        if (seMueve)
        {
            timerAnim += Time.deltaTime;
            if (timerAnim >= velocidadAnimacion)
            {
                timerAnim = 0;
                frameMovimiento = (frameMovimiento + 1) % 2;
            }
            PonerSprite(indiceDireccion, frameMovimiento);
        }
        else
        {
            frameMovimiento = 0;
            PonerSprite(indiceDireccion, 0);
        }
    }

    // --- AQUÍ ESTABA EL ERROR: HE RECUPERADO ESTE MÉTODO ---
    public void FaceDirection(Vector2 dir)
    {
        // Fuerza al tanque a mirar a una dirección inmediatamente (útil para el disparo)
        if (dir != Vector2.zero)
        {
            ActualizarVisuales(dir, false);
        }
    }
    // -------------------------------------------------------

    int ObtenerIndicePorAngulo(float a)
    {
        // 0º = Derecha, 90º = Arriba

        // Element 0: Noreste (0 a 90)
        if (a >= 0 && a < 90) return 0;

        // Element 3: Noroeste (90 a 180)
        if (a >= 90 && a < 180) return 3;

        // Element 2: Suroeste (180 a 270)
        if (a >= 180 && a < 270) return 2;

        // Element 1: Sureste (270 a 360)
        return 1;
    }

    void PonerSprite(int indice, int frame)
    {
        if (direcciones != null && indice < direcciones.Length)
        {
            var grupo = direcciones[indice];
            if (grupo.moveSprites != null && frame < grupo.moveSprites.Length)
            {
                spriteRenderer.sprite = grupo.moveSprites[frame];
            }
        }
    }

    // DISPARO
    public void TriggerShootAnim()
    {
        if (shootRoutine != null) StopCoroutine(shootRoutine);
        shootRoutine = StartCoroutine(AnimacionDisparo());
    }

    IEnumerator AnimacionDisparo()
    {
        isShooting = true;
        if (indiceDireccion < direcciones.Length)
        {
            Sprite[] sprites = direcciones[indiceDireccion].shootSprites;
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteRenderer.sprite = sprites[i];
                yield return new WaitForSeconds(velocidadDisparo);
            }
        }
        isShooting = false;
    }
}