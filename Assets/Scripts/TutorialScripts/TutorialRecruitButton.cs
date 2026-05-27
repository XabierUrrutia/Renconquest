using UnityEngine;
using UnityEngine.UI;

public class TutorialRecruitButton : MonoBehaviour
{
    [Header("Soldier")]
    public GameObject soldierPrefab;
    public Transform buildingTransform;

    [Header("Self")]
    public Button recruitButton;

    void Start()
    {
        if (recruitButton != null)
            recruitButton.onClick.AddListener(OnRecruitClicked);
    }

    void OnRecruitClicked()
    {
        if (soldierPrefab == null) return;

        Vector3 spawnPos = buildingTransform != null
            ? buildingTransform.position + Vector3.right * 1.5f
            : transform.position;

        Instantiate(soldierPrefab, spawnPos, Quaternion.identity);
        GameEvents.RaiseSoldierRecruited();

        if (recruitButton != null)
            recruitButton.gameObject.SetActive(false);
    }
}