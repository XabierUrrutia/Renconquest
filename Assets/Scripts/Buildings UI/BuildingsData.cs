using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "RTS/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identificação")]
    public string buildingName;
    public Sprite buildingSprite;
    public GameObject buildingPrefab;

    [Header("Economia")]
    [Tooltip("Custo em unidades de recurso para construir este edifício")]
    public int cost = 0;

    [Header("Construção")]
    [Tooltip("Tempo em segundos que demora a construir este edifício")]
    public float buildTime = 3f;

    [Header("Grid")]
    [Tooltip("Tamanho da célula do grid para snap")]
    public Vector2 gridSize = Vector2.one;
}