using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialNarrator : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI narratorText; // Texto onde as falas aparecem
    public Button nextButton;            // Botão para avançar a fala
    public GameObject panelToClose;      // Opcional: painel que contém o diálogo (será desativado ao fim)

    [Header("Falas (máx. 4)")]
    [TextArea(2, 4)] public string fala1;
    [TextArea(2, 4)] public string fala2;
    [TextArea(2, 4)] public string fala3;
    [TextArea(2, 4)] public string fala4;

    private string[] falas;
    private int index = 0;

    void Awake()
    {
        // Agrupa as 4 falas numa array para iteração simples
        falas = new string[4] { fala1 ?? "", fala2 ?? "", fala3 ?? "", fala4 ?? "" };

        // Se não foram atribuídos via Inspector, tenta encontrar componentes filhos
        if (narratorText == null)
            narratorText = GetComponentInChildren<TextMeshProUGUI>();
        if (nextButton == null)
            nextButton = GetComponentInChildren<Button>();

        if (nextButton != null)
        {
            // Garante que o listener está apenas uma vez
            nextButton.onClick.RemoveListener(NextLine);
            nextButton.onClick.AddListener(NextLine);
        }
    }

    void Start()
    {
        index = 0;
        ShowCurrentLine();
    }

    // Método público para ligar ao OnClick do botão (se preferir ligar manualmente)
    public void NextLine()
    {
        index++;
        // Ignora falas vazias ao avançar
        while (index < falas.Length && string.IsNullOrWhiteSpace(falas[index]))
            index++;

        if (index >= falas.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (narratorText == null) return;

        // Encontra a próxima fala não vazia a partir do índice atual
        int i = index;
        while (i < falas.Length && string.IsNullOrWhiteSpace(falas[i])) i++;

        if (i < falas.Length)
        {
            narratorText.text = falas[i];
            index = i;
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        // Desativa painel/opcionais e remove listener
        if (panelToClose != null)
            panelToClose.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextLine);

        // Alternativa: desativa este componente
        enabled = false;
    }

    // Permite reiniciar o diálogo via código se necessário
    public void RestartDialogue()
    {
        index = 0;
        enabled = true;
        if (panelToClose != null)
            panelToClose.SetActive(true);
        ShowCurrentLine();
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextLine);
            nextButton.onClick.AddListener(NextLine);
        }
    }

    // ----------------- Novos métodos públicos -----------------

    // Mostra a fala específica pelo seu índice (0-based: 0..4)
    public void ShowLineIndex(int falaIndex)
    {
        if (falas == null || falas.Length == 0) return;
        if (falaIndex < 0 || falaIndex >= falas.Length)
        {
            Debug.LogWarning($"TutorialNarrator: índice de fala inválido ({falaIndex}).");
            return;
        }

        if (string.IsNullOrWhiteSpace(falas[falaIndex]))
        {
            Debug.LogWarning($"TutorialNarrator: fala em índice {falaIndex} está vazia.");
            return;
        }

        index = falaIndex;
        if (narratorText != null)
            narratorText.text = falas[falaIndex];
    }

    // Mostra texto customizado (não presente nas 5 falas)
    public void ShowCustomText(string text)
    {
        if (narratorText == null) return;
        narratorText.text = text ?? "";
    }

    // Retorna quantas falas estão disponíveis (sempre 5 neste design)
    public int GetLineCount()
    {
        return falas != null ? falas.Length : 0;
    }
}