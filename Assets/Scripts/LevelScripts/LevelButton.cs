using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Tooltip("Índice do nível (1-based)")]
    public int levelIndex = 1;

    [Header("UI References (opcionais)")]
    public GameObject lockedIcon;
    public GameObject completedIcon;

    private Button btn;

    void Awake()
    {
        // Garantir que btn esteja inicializado antes de qualquer OnEnable/Refresh
        btn = GetComponent<Button>();
    }

    void Start()
    {
        if (btn != null)
            btn.onClick.AddListener(OnClick);

        Refresh();
    }

    void OnEnable()
    {
        // Atualiza sempre que o painel for reativado
        Refresh();
    }

    public void Refresh()
    {
        if (LevelManager.Instance == null) return;

        bool unlocked = LevelManager.Instance.IsUnlocked(levelIndex);
        bool completed = LevelManager.Instance.IsCompleted(levelIndex);

        if (lockedIcon != null) lockedIcon.SetActive(!unlocked);
        if (completedIcon != null) completedIcon.SetActive(completed);

        // Protege contra NullReference caso algo não tenha sido inicializado
        if (btn != null) btn.interactable = unlocked;
    }

    void OnClick()
    {
        SoundColector.Instance?.PlayUiClick();

        // Carrega o nível (poderias mostrar um painel de confirmação aqui)
        LevelManager.Instance.LoadLevel(levelIndex);
    }
}