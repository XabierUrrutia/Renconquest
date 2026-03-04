using UnityEngine;
using UnityEngine.Events;

public class EnemyDeathListener : MonoBehaviour
{
    public UnityEvent onEnemyDied;
    private bool _isInvoking = false;

    void Awake()
    {
        TrySubscribeToManager();
    }

    void Start()
    {
        TrySubscribeToManager();
    }

    private void TrySubscribeToManager()
    {
        var mgr = FindObjectOfType<TutorialStep2Manager>();
        if (mgr != null)
        {
            onEnemyDied.RemoveListener(mgr.OnEnemyDeath);
            onEnemyDied.AddListener(mgr.OnEnemyDeath);
            Debug.Log($"[EnemyDeathListener] '{gameObject.name}' subscrito ao TutorialStep2Manager");
        }
        else
        {
            Debug.LogWarning($"[EnemyDeathListener] Nenhum TutorialStep2Manager encontrado para '{gameObject.name}'");
        }
    }

    public void InvokeDeath()
    {
        if (_isInvoking) return;
        _isInvoking = true;

        Debug.Log($"[EnemyDeathListener] InvokeDeath() chamado em '{gameObject.name}'");
        onEnemyDied?.Invoke();

        _isInvoking = false;
    }
}
