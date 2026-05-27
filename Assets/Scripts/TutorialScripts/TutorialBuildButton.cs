using UnityEngine;
using UnityEngine.UI;

public class TutorialBuildButton : MonoBehaviour
{
    [Header("Building")]
    public GameObject buildingPrefab;
    public Transform spawnPoint;

    [Header("After Build")]
    public Button recruitButton;

    [Header("Self")]
    public Button buildButton;

    private GameObject spawnedBuilding;

    void Start()
    {
        if (recruitButton != null)
            recruitButton.gameObject.SetActive(false);

        if (buildButton != null)
            buildButton.onClick.AddListener(OnBuildClicked);
    }

    void OnBuildClicked()
    {
        if (buildingPrefab == null || spawnPoint == null) return;

        spawnedBuilding = Instantiate(buildingPrefab, spawnPoint.position, Quaternion.identity);
        GameEvents.RaiseBuildingPlaced(spawnedBuilding.transform);

        if (buildButton != null)
            buildButton.gameObject.SetActive(false);

        if (recruitButton != null)
        {
            recruitButton.gameObject.SetActive(true);
            var recruitScript = recruitButton.GetComponent<TutorialRecruitButton>();
            if (recruitScript != null && spawnedBuilding != null)
                recruitScript.buildingTransform = spawnedBuilding.transform;
        }
    }
}