using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    [Header("CONFIGURACIÓN DEL SPAWN")]
    public GameObject playerPrefab;
    public int cantidadSoldados = 3;
    public float radioSpawn = 1.5f;

    [Header("Referencia de Cámara")]
    public cameraFollow cameraScript;

    [Header("Formación de Spawn")]
    public bool usarFormacionCircular = true;

    private List<GameObject> soldadosSpawneados = new List<GameObject>();

    void Start()
    {
        // Buscar la cámara automáticamente si no está asignada
        if (cameraScript == null)
        {
            cameraScript = FindObjectOfType<cameraFollow>();
        }

        SpawnSoldados();
    }

    public void SpawnSoldados()
    {

        if (playerPrefab != null)
        {
            Vector3[] posicionesSpawn = CalcularPosicionesSpawn(transform.position, cantidadSoldados);

            for (int i = 0; i < cantidadSoldados; i++)
            {
                GameObject soldado = Instantiate(playerPrefab, posicionesSpawn[i], transform.rotation);

                // Añadimos a la lista para tener referencia, pero ya no los borraremos
                soldadosSpawneados.Add(soldado);
            }

            // Asignar cámara al último creado (opcional)
            if (cameraScript != null && soldadosSpawneados.Count > 0)
            {
                // cameraScript.target = soldadosSpawneados[0].transform; 
            }
        }
    }

    Vector3[] CalcularPosicionesSpawn(Vector3 posicionCentral, int cantidad)
    {
        Vector3[] posiciones = new Vector3[cantidad];

        if (cantidad == 1)
        {
            posiciones[0] = posicionCentral;
            return posiciones;
        }

        if (usarFormacionCircular)
        {
            // Formación circular perfecta
            for (int i = 0; i < cantidad; i++)
            {
                float angulo = i * (2f * Mathf.PI / cantidad);
                float x = Mathf.Cos(angulo) * radioSpawn;
                float y = Mathf.Sin(angulo) * radioSpawn;
                posiciones[i] = posicionCentral + new Vector3(x, y, 0);
            }
        }
        else
        {
            // Formación en triángulo para 3 unidades
            if (cantidad == 3)
            {
                posiciones[0] = posicionCentral + new Vector3(0, radioSpawn, 0); // Arriba
                posiciones[1] = posicionCentral + new Vector3(-radioSpawn * 0.866f, -radioSpawn * 0.5f, 0); // Abajo-izquierda
                posiciones[2] = posicionCentral + new Vector3(radioSpawn * 0.866f, -radioSpawn * 0.5f, 0); // Abajo-derecha
            }
            else
            {
                // Formación en cuadrícula para otras cantidades
                int filas = Mathf.CeilToInt(Mathf.Sqrt(cantidad));
                int columnas = Mathf.CeilToInt((float)cantidad / filas);

                int index = 0;
                for (int fila = 0; fila < filas; fila++)
                {
                    for (int columna = 0; columna < columnas; columna++)
                    {
                        if (index >= cantidad) break;

                        float x = (columna - (columnas - 1) * 0.5f) * radioSpawn;
                        float y = (fila - (filas - 1) * 0.5f) * radioSpawn;
                        posiciones[index] = posicionCentral + new Vector3(x, y, 0);
                        index++;
                    }
                }
            }
        }

        return posiciones;
    }

    // Método para respawn
    public void RespawnSoldados()
    {
        SpawnSoldados();
    }

    // Método para obtener todos los soldados (útil para otros scripts)
    public List<GameObject> GetSoldadosSpawneados()
    {
        return new List<GameObject>(soldadosSpawneados);
    }

    // Método para obtener el primer soldado
    public GameObject GetPrimerSoldado()
    {
        return soldadosSpawneados.Count > 0 ? soldadosSpawneados[0] : null;
    }

    // Visualizar el punto de spawn en el Editor
    void OnDrawGizmos()
    {
        // Punto central de spawn
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Dibujar las posiciones de spawn previstas
        Vector3[] posicionesPrevistas = CalcularPosicionesSpawn(transform.position, cantidadSoldados);

        Gizmos.color = Color.blue;
        foreach (Vector3 posicion in posicionesPrevistas)
        {
            Gizmos.DrawWireSphere(posicion, 0.2f);
            Gizmos.DrawLine(transform.position, posicion);
        }

        // Dibujar radio de spawn
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioSpawn);

        Gizmos.DrawIcon(transform.position + Vector3.up * (radioSpawn + 0.5f), "SpawnPoint.png", true);
    }

    // Visualizar más detalle cuando está seleccionado
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioSpawn);

        // Mostrar números de las posiciones de spawn
        Vector3[] posicionesPrevistas = CalcularPosicionesSpawn(transform.position, cantidadSoldados);
        for (int i = 0; i < posicionesPrevistas.Length; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(posicionesPrevistas[i], Vector3.one * 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(posicionesPrevistas[i] + Vector3.up * 0.2f, (i + 1).ToString());
#endif
        }
    }
}