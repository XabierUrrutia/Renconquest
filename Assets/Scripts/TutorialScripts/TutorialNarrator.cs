using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialNarrator : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI narratorText; // Texto onde as falas aparecem
    public Button nextButton;            // Bot�o para avan�ar a fala
    public GameObject panelToClose;      // Opcional: painel que cont�m o di�logo (ser� desativado ao fim)

    [Header("Falas (m�x. 5)")]
    [Header("Behaviour")]
    [Tooltip("Uncheck to disable auto-dialog on Start. Use when another script drives the narrator.")]
    public bool autoStart = true;

    [TextArea(2, 4)] public string fala1;
    [TextArea(2, 4)] public string fala2;
    [TextArea(2, 4)] public string fala3;
    [TextArea(2, 4)] public string fala4;
    [TextArea(2, 4)] public string fala5;

    private string[] falas;
    private int index = 0;

    void Awake()
    {
        // Agrupa as 5 falas numa array para itera��o simples
        falas = new string[5] { fala1 ?? "", fala2 ?? "", fala3 ?? "", fala4 ?? "", fala5 ?? "" };

        // Se n�o foram atribu�dos via Inspector, tenta encontrar componentes filhos
        if (narratorText == null)
            narratorText = GetComponentInChildren<TextMeshProUGUI>();
        if (nextButton == null)
            nextButton = GetComponentInChildren<Button>();

        if (nextButton != null)
        {
            // Garante que o listener est� apenas uma vez
            nextButton.onClick.RemoveListener(NextLine);
            nextButton.onClick.AddListener(NextLine);
        }
    }

    void Start()
    {
        if (!autoStart) return;
        index = 0;
        ShowCurrentLine();
    }

    // M�todo p�blico para ligar ao OnClick do bot�o (se preferir ligar manualmente)
    public void NextLine()
    {
        index++;
        // Ignora falas vazias ao avan�ar
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

        // Encontra a pr�xima fala n�o vazia a partir do �ndice atual
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

    // Permite reiniciar o di�logo via c�digo se necess�rio
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

    // ----------------- Novos m�todos p�blicos -----------------

    // Mostra a fala espec�fica pelo seu �ndice (0-based: 0..4)
    public void ShowLineIndex(int falaIndex)
    {
        if (falas == null || falas.Length == 0) return;
        if (falaIndex < 0 || falaIndex >= falas.Length)
        {
            Debug.LogWarning($"TutorialNarrator: �ndice de fala inv�lido ({falaIndex}).");
            return;
        }

        if (string.IsNullOrWhiteSpace(falas[falaIndex]))
        {
            Debug.LogWarning($"TutorialNarrator: fala em �ndice {falaIndex} est� vazia.");
            return;
        }

        index = falaIndex;
        if (narratorText != null)
            narratorText.text = falas[falaIndex];
    }

    // Mostra texto customizado (n�o presente nas 5 falas)
    public void ShowCustomText(string text)
    {
        if (narratorText == null) return;
        narratorText.text = text ?? "";
    }

    // Retorna quantas falas est�o dispon�veis (sempre 5 neste design)
    public int GetLineCount()
    {
        return falas != null ? falas.Length : 0;
    }
}