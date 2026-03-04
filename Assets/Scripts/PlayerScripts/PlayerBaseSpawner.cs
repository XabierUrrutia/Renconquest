using UnityEngine;

/// <summary>
/// Spawner responsável por instanciar o prefab da PlayerBase na cena.
/// - Atribua o `playerBasePrefab` no Inspector.
/// - Se `spawnOnStart` for true, instancia automaticamente em Start().
/// - Fornece API pública `SpawnBase()` e `DestroyBase()`.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBaseSpawner : MonoBehaviour
{
    [Header("Prefab e Posição")]
    [Tooltip("Prefab da PlayerBase (deve ter o componente PlayerBase)")]
    public GameObject playerBasePrefab;
    [Tooltip("Transform onde a base será instanciada. Se vazio, usa a posição deste GameObject.")]
    public Transform spawnPoint;

    [Header("Comportamento")]
    [Tooltip("Se true, instancia automaticamente no Start()")]
    public bool spawnOnStart = true;
    [Tooltip("Se true, o GameObject instanciado será marcado como root (sem parent)")]
    public bool detachInstance = true;

    private GameObject spawnedBase;

    void Start()
    {
        if (spawnOnStart)
            SpawnBase();
    }

    /// <summary>
    /// Instancia a PlayerBase. Se já existir uma instância anterior, será removida primeiro.
    /// Retorna o GameObject instanciado (ou null se falhar).
    /// </summary>
    public GameObject SpawnBase()
    {
        if (playerBasePrefab == null)
        {
            Debug.LogError("[PlayerBaseSpawner] playerBasePrefab não atribuído.");
            return null;
        }

        // Remove instância anterior, se existir
        if (spawnedBase != null)
        {
            Destroy(spawnedBase);
            spawnedBase = null;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        spawnedBase = Instantiate(playerBasePrefab, pos, rot);
        spawnedBase.name = playerBasePrefab.name;

        if (detachInstance)
            spawnedBase.transform.SetParent(null);
        else
            spawnedBase.transform.SetParent(transform, true);

        Debug.Log($"[PlayerBaseSpawner] Base instanciada: {spawnedBase.name} em {pos}");
        return spawnedBase;
    }

    /// <summary>
    /// Destroi a base instanciada pelo spawner (se houver).
    /// </summary>
    public void DestroyBase()
    {
        if (spawnedBase != null)
        {
            Destroy(spawnedBase);
            spawnedBase = null;
            Debug.Log("[PlayerBaseSpawner] Base destruída via DestroyBase().");
        }
    }

    /// <summary>
    /// Retorna a instância atual da base (ou null).
    /// </summary>
    public GameObject GetSpawnedBase()
    {
        return spawnedBase;
    }
}