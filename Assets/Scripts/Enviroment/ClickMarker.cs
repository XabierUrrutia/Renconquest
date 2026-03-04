using UnityEngine;

public class ClickMarker : MonoBehaviour
{
    void Start()
    {
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * 0.3f, 10f * Time.deltaTime);
        Color c = GetComponent<SpriteRenderer>().color;
        c.a -= Time.deltaTime * 2f;
        GetComponent<SpriteRenderer>().color = c;

        if (c.a <= 0)
            Destroy(gameObject);
    }
}
