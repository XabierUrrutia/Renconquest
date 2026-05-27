using UnityEngine;
using UnityEngine.UI;

public class BuildingScreen : MonoBehaviour
{
    [Tooltip("Arraste aqui o GameObject UI que representa o painel de BUILD (inicialmente inativo)")]
    public GameObject buildPanelUI;

    [Tooltip("Bot�o que ir� abrir/fechar o painel (opcional). Se atribu�do, o listener ser� ligado automaticamente.")]
    public Button toggleButton;

    // HE ELIMINADO 'pauseOnOpen' y '_previousTimeScale' PORQUE YA NO LOS NECESITAS

    private bool _shown = false;

    void Start()
    {
        if (buildPanelUI != null)
            buildPanelUI.SetActive(false);

        if (toggleButton != null)
        {
            // garante que n�o adicionamos m�ltiplos listeners
            toggleButton.onClick.RemoveListener(ToggleBuild);
            toggleButton.onClick.AddListener(ToggleBuild);
        }
    }

    void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleBuild);
    }

    // Abre o painel de constru��o
    public void ShowBuild()
    {
        if (_shown) return;
        _shown = true;

        if (buildPanelUI != null)
            buildPanelUI.SetActive(true);

        SoundColector.Instance?.PlayUiPanelOpen();

        // AQU� ELIMIN� LA L�NEA QUE HAC�A Time.timeScale = 0f;

        Debug.Log("[BuildingScreen] Painel de constru��o mostrado.");
    }

    // Fecha o painel de constru��o
    public void HideBuild()
    {
        if (!_shown) return;
        _shown = false;

        if (buildPanelUI != null)
            buildPanelUI.SetActive(false);

        // AQU� ELIMIN� LA L�NEA QUE RESTAURABA EL TIEMPO

        Debug.Log("[BuildingScreen] Painel de constru��o ocultado.");
    }

    // Alterna estado (o mesmo bot�o abre e fecha)
    public void ToggleBuild()
    {
        SoundColector.Instance?.PlayUiClick();
        // Read actual panel state instead of relying on _shown, in case it was closed externally (e.g. X button)
        bool actuallyShown = buildPanelUI != null && buildPanelUI.activeSelf;
        _shown = actuallyShown;
        if (_shown) HideBuild();
        else ShowBuild();
    }

    public bool IsShown()
    {
        return buildPanelUI != null && buildPanelUI.activeSelf;
    }
}