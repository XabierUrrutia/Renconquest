using UnityEngine;

public class Medkit : MonoBehaviour
{
    [Header("Configuraci�n de Curaci�n")]
    [SerializeField] private int fixedHealAmount = 2; // Cura exactamente 2 puntos
    [SerializeField] private bool destroyOnUse = true;

    [Header("Efectos")]
    [SerializeField] private AudioClip healSound;
    [SerializeField] private GameObject healEffect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyHealToAll();
        }
    }

    private void ApplyHealToAll()
    {
        PlayerHealth[] soldiers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        GeneralHealth[] generals = FindObjectsByType<GeneralHealth>(FindObjectsSortMode.None);

        bool anyHealed = false;

        foreach (PlayerHealth ph in soldiers)
        {
            if (!ph.IsDead && !ph.IsFullHealth())
            {
                ph.Heal(fixedHealAmount);
                anyHealed = true;
            }
        }

        foreach (GeneralHealth gh in generals)
        {
            if (!gh.IsDead && !gh.IsFullHealth())
            {
                gh.Heal(fixedHealAmount);
                anyHealed = true;
            }
        }

        if (!anyHealed) return;

        PlayHealEffects();
        GameEvents.RaiseMedikitPickedUp();

        if (destroyOnUse)
        {
            Destroy(gameObject);
        }
        else
        {
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void PlayHealEffects()
    {
        if (healSound != null)
        {
            AudioSource.PlayClipAtPoint(healSound, transform.position);
        }

        if (healEffect != null)
        {
            Instantiate(healEffect, transform.position, Quaternion.identity);
        }
    }
}