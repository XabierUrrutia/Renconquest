using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class GenericCloseButton : MonoBehaviour
{
    [Header("Painéis a fechar")]
    [Tooltip("Lista de GameObjects (painéis/menus) que serão desativados quando clicar neste botão.")]
    public GameObject[] panelsToClose;

    [Header("Opções")]
    [Tooltip("Se true, reativa Time.timeScale = 1 ao fechar (útil se algum menu tiver pausado o jogo).")]
    public bool resumeTimeOnClose = true;

    private Button closeButton;

    void Awake()
    {
        closeButton = GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnCloseClicked()
    {
        SoundColector.Instance?.PlayUiClick();

        // Cancelar selección de edificio si está en modo construcción
        if (BuildingManager.Instance != null)
            BuildingManager.Instance.CancelPlacing();

        if (panelsToClose != null)
        {
            for (int i = 0; i < panelsToClose.Length; i++)
            {
                if (panelsToClose[i] != null)
                    panelsToClose[i].SetActive(false);
            }
        }

        if (resumeTimeOnClose)
        {
            Time.timeScale = 1f;
        }
    }
}