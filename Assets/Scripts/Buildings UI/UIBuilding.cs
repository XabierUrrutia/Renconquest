// Building.cs
using UnityEngine;

public class UIBuilding : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;

    private void Start()
    {
        // Inicialização do edifício
        GetComponent<SpriteRenderer>().sprite = buildingData.buildingSprite;
    }
}