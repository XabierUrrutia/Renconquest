using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class InputBindingsLoader : MonoBehaviour
{
    [Tooltip("Asset de InputActions usado no jogo (o mesmo Controls.inputactions usado no menu).")]
    public InputActionAsset inputActionsAsset;

    [Tooltip("Nome do ActionMap de gameplay (exactamente como no asset, por ex. \"Gameplay\").")]
    public string gameplayMapName = "Gameplay";

    void Awake()
    {
        if (inputActionsAsset == null)
        {
            Debug.LogWarning("[InputBindingsLoader] inputActionsAsset não atribuído.");
            return;
        }

        var map = inputActionsAsset.FindActionMap(gameplayMapName, throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogWarning($"[InputBindingsLoader] ActionMap '{gameplayMapName}' não encontrado no asset.");
            return;
        }

        // Carregar overrides guardados
        string key = $"Bindings_{map.name}";
        var savedJson = PlayerPrefs.GetString(key, string.Empty);
        if (!string.IsNullOrEmpty(savedJson))
        {
            map.LoadBindingOverridesFromJson(savedJson);
            Debug.Log($"[InputBindingsLoader] Overrides carregados para map '{map.name}' com chave '{key}'.");
        }

        // Ativar o mapa (se ainda não estiver ativado por PlayerInput)
        map.Enable();
    }
}