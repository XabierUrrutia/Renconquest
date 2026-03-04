using UnityEngine;

public class TutorialEnemyHealth : MonoBehaviour
{
    public int maxHp = 10;
    int hp;

    void Awake()
    {
        hp = maxHp;
        Debug.Log($"[TutorialEnemyHealth] Awake() '{gameObject.name}' hp={hp}");
    }

    public void TakeDamage(int amount)
    {
        Debug.Log($"[TutorialEnemyHealth] TakeDamage({amount}) chamado em '{gameObject.name}' (hp antes={hp})");
        hp -= amount;
        if (hp <= 0)
            Die();
    }

    void Die()
    {
        var listener = GetComponent<EnemyDeathListener>();
        if (listener != null)
        {
            listener.InvokeDeath(); // chama evento ANTES de destruir
            Debug.Log($"[TutorialEnemyHealth] '{gameObject.name}' morreu — evento disparado");
        }
        else
        {
            Debug.LogWarning($"[TutorialEnemyHealth] '{gameObject.name}' não tem EnemyDeathListener");
        }

        Destroy(gameObject);
    }
}
