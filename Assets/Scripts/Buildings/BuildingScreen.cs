using UnityEngine;
using UnityEngine.UI;

public class BuildingScreen : MonoBehaviour
{
    [Tooltip("Arraste aqui o GameObject UI que representa o painel de BUILD (inicialmente inativo)")]
    public GameObject buildPanelUI;

    [Tooltip("Botão que irá abrir/fechar o painel (opcional). Se atribuído, o listener será ligado automaticamente.")]
    public Button toggleButton;

    // HE ELIMINADO 'pauseOnOpen' y '_previousTimeScale' PORQUE YA NO LOS NECESITAS

    private bool _shown = false;

    void Start()
    {
        if (buildPanelUI != null)
            buildPanelUI.SetActive(false);

        if (toggleButton != null)
        {
            // garante que não adicionamos múltiplos listeners
            toggleButton.onClick.RemoveListener(ToggleBuild);
            toggleButton.onClick.AddListener(ToggleBuild);
        }
    }

    void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleBuild);
    }

    // Abre o painel de construção
    public void ShowBuild()
    {
        if (_shown) return;
        _shown = true;

        if (buildPanelUI != null)
            buildPanelUI.SetActive(true);

        SoundColector.Instance?.PlayUiPanelOpen();

        // AQUÍ ELIMINÉ LA LÍNEA QUE HACÍA Time.timeScale = 0f;

        Debug.Log("[BuildingScreen] Painel de construção mostrado.");
    }

    // Fecha o painel de construção
    public void HideBuild()
    {
        if (!_shown) return;
        _shown = false;

        if (buildPanelUI != null)
            buildPanelUI.SetActive(false);

        // AQUÍ ELIMINÉ LA LÍNEA QUE RESTAURABA EL TIEMPO

        Debug.Log("[BuildingScreen] Painel de construção ocultado.");
    }

    // Alterna estado (o mesmo botão abre e fecha)
    public void ToggleBuild()
    {
        SoundColector.Instance?.PlayUiClick();
        if (_shown) HideBuild();
        else ShowBuild();
    }

    // Retorna se o painel está aberto
    public bool IsShown()
    {
        return _shown;
    }
}