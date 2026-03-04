using UnityEngine;

public class BulletEnemy : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public Rigidbody2D rb;
    public bool isEnemyBullet = false;
    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogWarning("[Bullet] rb é null no Start!");
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, 5f);
        Debug.Log($"[Bullet] Start() - '{gameObject.name}' velocity={rb?.linearVelocity} isEnemyBullet={isEnemyBullet}");
    }

    void OnTriggerEnter2D(Collider2D hit)
    {
        Debug.Log($"[Bullet] OnTriggerEnter2D: '{gameObject.name}' colidiu com '{hit.gameObject.name}' (tag='{hit.gameObject.tag}') - isEnemyBullet={isEnemyBullet}");

        if (hit.CompareTag("Enemy") && !isEnemyBullet)
        {
            // procura ambos os componentes possíveis
            var tutorialEnemy = hit.GetComponent<TutorialEnemyHealth>();
            var enemyOld = hit.GetComponent<EnemyHealth>(); // se existir por acaso

            if (tutorialEnemy != null)
            {
                Debug.Log($"[Bullet] Encontrado TutorialEnemyHealth em '{hit.name}' - aplicando dano {damage}");
                tutorialEnemy.TakeDamage(damage);
            }
            else if (enemyOld != null)
            {
                Debug.Log($"[Bullet] Encontrado EnemyHealth (antigo) em '{hit.name}' - aplicando dano {damage}");
                enemyOld.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[Bullet] O objeto '{hit.name}' NÃO tem TutorialEnemyHealth nem EnemyHealth!");
            }

            Destroy(gameObject);
            return;
        }

        if (hit.CompareTag("Player") && isEnemyBullet)
        {
            var player = hit.GetComponent<PlayerHealth>();
            if (player != null)
            {
                Debug.Log($"[Bullet] BALA INIMIGA: acertou no Player '{hit.name}'");
                player.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }
    }
}
