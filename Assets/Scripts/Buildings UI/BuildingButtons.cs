// BuildingButton.cs
using UnityEngine;
using UnityEngine.UI;

public class BuildingButtons : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
        SoundColector.Instance?.PlayUiClick();


        // Opcional: Definir ícone do botão
        if (buildingData.buildingSprite != null)
        {
            button.image.sprite = buildingData.buildingSprite;
        }
    }

    private void OnButtonClick()
    {
        BuildingManager.Instance.SelectBuilding(buildingData);
    }
}