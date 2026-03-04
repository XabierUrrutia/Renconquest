using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("Índice do passo que este trigger completa (0-based)")]
    public int stepIndex = 0;

    [Tooltip("Se true, o trigger só funciona uma vez")]
    public bool singleUse = true;

    [Header("Eventos do Inspector")]
    [Tooltip("Eventos a executar quando o jogador entra no trigger (pode atribuir métodos no Inspector)")]
    public UnityEvent onPlayerEnter;

    [Header("Detecção por Tag")]
    [Tooltip("Tag que identifica o jogador (usado na comparação direta)")]
    public string playerTag = "Player";

    [Tooltip("Se preenchido, o GameObject do trigger (ex.: a torre) deve ter esta tag para o trigger funcionar. Deixa vazio para ignorar.")]
    public string requiredThisTag = "Tower";

    [Tooltip("Se true, aceita colisores filhos do jogador (procura tag no parent/root)")]
    public bool allowParentTagCheck = true;

    [Header("Fallbacks (opcionais)")]
    [Tooltip("Se true, aceita objectos que tenham PlayerHealth em qualquer ancestor (útil quando coliders estão em filhos)")]
    public bool allowPlayerHealthCheck = true;

    [Header("Auto-correções")]
    [Tooltip("Se true, se o jogador não tiver Rigidbody2D o script adiciona um Rigidbody2D kinematic automaticamente (útil em protótipo).")]
    public bool autoAddRigidbodyToPlayer = true;

    [Header("Mensagem de passo")]
    [Tooltip("Texto TMP que exibirá a mensagem 'Passaste o passo X' (opcional)")]
    public TextMeshProUGUI stepMessageText;
    [Tooltip("Duração em segundos da mensagem na tela")]
    public float messageDuration = 2f;

    void Reset()
    {
        // garante que é trigger no editor ao adicionar
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
            Debug.LogError($"TutorialTrigger precisa de um Collider2D em '{name}'");
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"TutorialTrigger '{name}': Collider2D não estava em 'Is Trigger'. Foi ativado automaticamente.");
        }

        // Aviso útil se requiredThisTag estiver preenchido mas o objeto não tiver a tag
        if (!string.IsNullOrEmpty(requiredThisTag) && !gameObject.CompareTag(requiredThisTag))
        {
            Debug.LogWarning($"TutorialTrigger '{name}': expected tag '{requiredThisTag}' on this GameObject but actual tag is '{gameObject.tag}'. Ajusta no Inspector ou coloca a tag '{requiredThisTag}' no objecto.");
        }

        // Verificação e correção comum: existe o jogador com tag? tem Collider2D e Rigidbody2D?
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                Debug.LogWarning($"TutorialTrigger '{name}': não foi encontrado nenhum GameObject com a tag '{playerTag}'. Verifica a tag do jogador.");
            }
            else
            {
                Collider2D playerCol = player.GetComponent<Collider2D>() ?? player.GetComponentInChildren<Collider2D>();
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>() ?? player.GetComponentInChildren<Rigidbody2D>();

                if (playerCol == null)
                {
                    Debug.LogWarning($"TutorialTrigger '{name}': o jogador '{player.name}' não parece ter um Collider2D. Adiciona um Collider2D (ex.: CircleCollider2D) ao jogador para que triggers funcionem.");
                }

                if (playerRb == null)
                {
                    if (autoAddRigidbodyToPlayer && playerCol != null)
                    {
                        // Adiciona Rigidbody2D e configura como kinematic para não interferir com movement code baseado em transform
                        var rb = player.AddComponent<Rigidbody2D>();
                        rb.bodyType = RigidbodyType2D.Kinematic;
                        rb.simulated = true;
                        Debug.Log($"TutorialTrigger '{name}': adicionou Rigidbody2D kinematic temporário ao jogador '{player.name}' porque estava ausente. Para produção, adiciona um Rigidbody2D no prefab do jogador.");
                    }
                    else
                    {
                        Debug.LogWarning($"TutorialTrigger '{name}': o jogador '{player.name}' não tem Rigidbody2D. Pelo menos um dos dois colliders (trigger ou jogador) deve ter Rigidbody2D para que OnTrigger funcione.");
                    }
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnter(other.gameObject);
    }

    // fallback caso o collider não esteja como trigger (opcional)
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleEnter(collision.gameObject);
    }

    // Método público para testar via Inspector ou botão
    public void SimulateEnter()
    {
        Debug.Log($"TutorialTrigger '{name}': SimulateEnter chamado para step {stepIndex}");
        StartCoroutine(ShowStepMessageCoroutine());
        InvokeActions();
    }

    bool IsPlayerByTag(GameObject go)
    {
        if (go == null) return false;

        // Tag direta
        if (!string.IsNullOrEmpty(playerTag) && go.CompareTag(playerTag))
            return true;

        // Tag em ancestor/root
        if (allowParentTagCheck)
        {
            Transform t = go.transform;
            while (t != null)
            {
                if (!string.IsNullOrEmpty(playerTag) && t.gameObject.CompareTag(playerTag))
                    return true;
                t = t.parent;
            }
        }

        return false;
    }

    bool IsPlayerByFallbacks(GameObject go)
    {
        if (go == null) return false;

        if (allowPlayerHealthCheck && go.GetComponentInParent<PlayerHealth>() != null)
            return true;

        return false;
    }

    void HandleEnter(GameObject otherGO)
    {
        if (otherGO == null) return;

        // DEBUG: imprime infos úteis para diagnosticar
        Debug.Log($"TutorialTrigger '{name}': OnEnter detectado por '{otherGO.name}'. Tag: '{otherGO.tag}'. Root Tag: '{otherGO.transform.root.tag}'. " +
                  $"HasPlayerHealthParent={(otherGO.GetComponentInParent<PlayerHealth>() != null)} HasRigidbody={(otherGO.GetComponent<Rigidbody2D>() != null)}");

        // Se requisitado, valida tag do próprio trigger (ex.: "Tower")
        if (!string.IsNullOrEmpty(requiredThisTag) && !gameObject.CompareTag(requiredThisTag))
        {
            Debug.Log($"TutorialTrigger '{name}': Trigger não tem a tag requerida '{requiredThisTag}'. Ignorando.");
            return;
        }

        // Primeiro tentativa: identificação por tags (prioritária)
        if (IsPlayerByTag(otherGO))
        {
            TriggerActivated(otherGO);
            return;
        }

        // Se tag não detectada, tenta fallbacks opcionais (componentes)
        if (IsPlayerByFallbacks(otherGO))
        {
            TriggerActivated(otherGO);
            return;
        }

        Debug.Log($"TutorialTrigger '{name}': '{otherGO.name}' não identificado como jogador (tag '{playerTag}' ausente e fallbacks falharam). Ignorando.");
    }

    void TriggerActivated(GameObject playerGO)
    {
        // Invoca callbacks do Inspector — managers por cena devem subscrever aqui
        if (onPlayerEnter != null)
            onPlayerEnter.Invoke();

        // Mostra na tela a mensagem de passo concluído (se estiver configurado)
        StartCoroutine(ShowStepMessageCoroutine());

        if (singleUse)
            gameObject.SetActive(false);
    }

    void InvokeActions()
    {
        if (onPlayerEnter != null)
            onPlayerEnter.Invoke();
    }

    IEnumerator ShowStepMessageCoroutine()
    {
        if (stepMessageText == null) yield break;

        string original = stepMessageText.text;
        stepMessageText.text = $"Passaste o passo {stepIndex + 1}!";
        stepMessageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        stepMessageText.text = original;
        // opcional: esconder o campo após mensagem
        // stepMessageText.gameObject.SetActive(false);
    }
}