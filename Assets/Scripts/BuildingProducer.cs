using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BuildingProducer : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("El Slider que está dentro del Canvas WorldSpace de este edificio")]
    public Slider progressSlider;

    [Header("Configuración de Spawn")]
    [Tooltip("Punto central donde aparecerán las unidades.")]
    public Transform spawnPoint;

    [Tooltip("Radio del área donde aparecerán las unidades para que no se amontonen.")]
    public float spawnRadius = 1.5f; // <--- NUEVA VARIABLE

    [Header("Estado")]
    public bool isBusy = false;

    private void Awake()
    {
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(false);
            progressSlider.value = 0;
        }
    }

    public void StartProduction(BuildingData unitData)
    {
        if (isBusy) return;
        StartCoroutine(ProductionRoutine(unitData));
    }

    IEnumerator ProductionRoutine(BuildingData unitData)
    {
        isBusy = true;

        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0f;
        }

        float timer = 0f;
        float totalTime = unitData.buildTime;

        while (timer < totalTime)
        {
            timer += Time.deltaTime;
            if (progressSlider != null)
                progressSlider.value = timer / totalTime;
            yield return null;
        }

        if (progressSlider != null) progressSlider.gameObject.SetActive(false);

        SpawnUnitInArea(unitData); // <--- LLAMAMOS A LA NUEVA FUNCIÓN

        isBusy = false;
    }

    // --- FUNCIÓN DE SPAWN MEJORADA ---
    void SpawnUnitInArea(BuildingData unitData)
    {
        if (unitData.buildingPrefab == null) return;

        // 1. Determinar el centro del spawn
        Vector3 centerPos = transform.position + new Vector3(2f, 0f, 0f); // Default por si no hay spawnPoint
        if (spawnPoint != null) centerPos = spawnPoint.position;

        // 2. Calcular un punto aleatorio dentro del radio
        // Random.insideUnitCircle devuelve un Vector2 (X, Y) aleatorio dentro de un radio de 1.
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;

        // 3. Sumar el offset a la posición central
        // Nota: Como es un juego 2D (usas Collider2D), sumamos en X e Y.
        Vector3 finalPos = centerPos + new Vector3(randomOffset.x, randomOffset.y, 0f);

        // 4. Instanciar
        GameObject unit = Instantiate(unitData.buildingPrefab, finalPos, Quaternion.identity);

        Debug.Log($"[{name}] Unidad desplegada en área: {unit.name}");
    }

    // --- AYUDA VISUAL EN EL EDITOR ---
    // Esto dibujará un círculo amarillo en la escena para que veas dónde saldrán
    private void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
        }
    }
}