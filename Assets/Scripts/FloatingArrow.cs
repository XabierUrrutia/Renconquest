using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.2f; // altura do salto
    [SerializeField] private float frequency = 2f;   // velocidade do salto

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float newY = Mathf.Sin(Time.time * frequency) * amplitude;

        Vector3 pos = transform.localPosition;
        pos.y = startPos.y + newY;

        transform.localPosition = pos;
    }
}
