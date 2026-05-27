using UnityEngine;
using UnityEngine.UI;

public class BuildingButtons : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;

    [Tooltip("Max number of this building the player can place. 0 = unlimited.")]
    [SerializeField] private int maxAllowed = 0;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);

        if (buildingData != null && buildingData.buildingSprite != null)
            button.image.sprite = buildingData.buildingSprite;
    }

    private void Update()
    {
        if (maxAllowed <= 0 || button == null || BuildingManager.Instance == null) return;
        button.interactable = BuildingManager.Instance.GetPlacedCount(buildingData) < maxAllowed;
    }

    private void OnButtonClick()
    {
        BuildingManager.Instance.SelectBuilding(buildingData);
    }
}