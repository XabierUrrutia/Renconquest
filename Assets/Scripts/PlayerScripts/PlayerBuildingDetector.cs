using UnityEngine;
using System.Collections.Generic;

public class PlayerBuildingDetector : MonoBehaviour
{
    [Header("Configuración")]
    public float detectionRange = 5f;

    private List<Building> allBuildings = new List<Building>();
    private EnemyBase currentEnemyBase = null;
    private Building currentBuilding = null;

    void Start()
    {
        Building[] buildingsArray = FindObjectsOfType<Building>();
        allBuildings = new List<Building>(buildingsArray);
        Debug.Log($"[{gameObject.name}] Encontró {allBuildings.Count} edificios");
    }

    void Update()
    {
        CheckBuildingsInRange();
        CheckEnemyBaseInRange();
    }

    void CheckBuildingsInRange()
    {
        Building closestBuilding = null;
        float closestDistance = Mathf.Infinity;

        foreach (Building building in allBuildings)
        {
            if (building == null || building.isConquered) continue;

            float distance = Vector2.Distance(transform.position, building.transform.position);

            if (distance <= building.conquestRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestBuilding = building;
            }
        }

        // Si cambió el edificio más cercano
        if (currentBuilding != closestBuilding)
        {
            // Desregistrarse del edificio anterior
            if (currentBuilding != null)
            {
                currentBuilding.UnregisterPlayer(this);
                Debug.Log($"[{gameObject.name}] Dejó de conquistar: {currentBuilding.gameObject.name}");
            }

            // Registrarse en el nuevo edificio
            currentBuilding = closestBuilding;
            if (currentBuilding != null)
            {
                currentBuilding.RegisterPlayer(this);
                Debug.Log($"[{gameObject.name}] Empezó a conquistar: {currentBuilding.gameObject.name}");
            }
        }

        // Si no hay edificios en rango, asegurarse de que currentBuilding es null
        if (closestBuilding == null && currentBuilding != null)
        {
            currentBuilding.UnregisterPlayer(this);
            currentBuilding = null;
        }
    }

    void CheckEnemyBaseInRange()
    {
        // Buscar todas las bases enemigas en la escena
        EnemyBase[] enemyBases = FindObjectsOfType<EnemyBase>();
        EnemyBase closestBase = null;
        float closestDistance = Mathf.Infinity;

        foreach (EnemyBase enemyBase in enemyBases)
        {
            if (enemyBase == null || enemyBase.isConquered) continue;

            float distance = Vector2.Distance(transform.position, enemyBase.transform.position);

            if (distance <= enemyBase.conquestRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestBase = enemyBase;
            }
        }

        // Si cambió la base más cercana
        if (currentEnemyBase != closestBase)
        {
            // Desregistrarse de la base anterior
            if (currentEnemyBase != null)
            {
                currentEnemyBase.UnregisterPlayer(this);
            }

            // Registrarse en la nueva base
            currentEnemyBase = closestBase;
            if (currentEnemyBase != null)
            {
                currentEnemyBase.RegisterPlayer(this);
            }
        }

        // Si no hay bases en rango, asegurarse de que currentEnemyBase es null
        if (closestBase == null && currentEnemyBase != null)
        {
            currentEnemyBase.UnregisterPlayer(this);
            currentEnemyBase = null;
        }
    }

    void OnDestroy()
    {
        if (currentBuilding != null)
        {
            currentBuilding.UnregisterPlayer(this);
        }
        if (currentEnemyBase != null)
        {
            currentEnemyBase.UnregisterPlayer(this);
        }
    }

    void OnDisable()
    {
        if (currentBuilding != null)
        {
            currentBuilding.UnregisterPlayer(this);
        }
        if (currentEnemyBase != null)
        {
            currentEnemyBase.UnregisterPlayer(this);
        }
    }
}