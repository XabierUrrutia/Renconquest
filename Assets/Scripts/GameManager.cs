using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // --- CONFIGURACI�N DE DERROTA ---
    [Header("Condiciones de Derrota")]
    // IMPORTANTE: Pon aqu� el precio de lo M�S BARATO que se pueda comprar en tu juego
    public int costeMinimoParaJugar = 100;
    public bool baseDestruida = false;
    // ---------------------------------------

    private List<IHealth> allUnits = new List<IHealth>();
    private bool gameOver = false;
    public static bool tutorialMode = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- NUEVO: COMPROBACI�N CONSTANTE (El secreto para que sea instant�neo) ---
    void Update()
    {
        if (gameOver) return;
        if (tutorialMode) return;

        // 1. SI LA BASE CAY� -> FIN DIRECTO
        if (baseDestruida)
        {
            // Solo lo llamamos una vez
            Debug.Log("�Base Principal destruida! Fin del juego.");
            TriggerGameOver();
            return;
        }

        // 2. COMPROBACI�N CONTINUA DE RECURSOS Y TROPAS
        // Solo verificamos si existen los managers para evitar errores
        if (MoneyManager.Instance != null)
        {
            int dineroActual = MoneyManager.Instance.CurrentMoney;

            // Calculamos tropas vivas
            int tropasMoviles = ContarTropasVivas();

            // CONDICI�N CR�TICA:
            // Si el dinero es menor que el coste m�nimo Y tengo 0 tropas...
            if (dineroActual < costeMinimoParaJugar && tropasMoviles <= 0)
            {
                Debug.Log("GAME OVER INSTANT�NEO: Sin tropas y sin dinero.");
                TriggerGameOver();
            }
        }
    }

    // M�todo auxiliar para contar tropas (Extra�do para usarlo en el Update limpiamente)
    private int ContarTropasVivas()
    {
        int count = 0;

        // Opci�n A: PopulationManager (M�s r�pido y fiable)
        if (PopulationManager.Instance != null)
        {
            count = PopulationManager.Instance.soldadosActuales + PopulationManager.Instance.tanquesActuales;
        }
        // Opci�n B: Respaldo manual (Tu c�digo original)
        else
        {
            // Limpieza preventiva de nulos
            // Nota: En un Update esto puede ser pesado, pero si no hay PopulationManager es necesario.
            for (int i = allUnits.Count - 1; i >= 0; i--)
            {
                var unit = allUnits[i];
                if (unit == null || (unit is Object obj && !obj) || unit.IsDead)
                {
                    allUnits.RemoveAt(i);
                    continue;
                }

                // Si NO tiene el script EsEdificio, asumimos que es tropa
                if (unit.transform.GetComponent<EsEdificio>() == null)
                {
                    count++;
                }
            }
        }
        return count;
    }
    // -----------------------------------------------------------------------

    public void RegisterUnit(IHealth unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
        }
    }

    public void UnregisterUnit(IHealth unit)
    {
        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
            // Ya no hace falta llamar a CheckGameOver aqu�, el Update lo cazar� al instante
        }
    }

    public void AddNewUnit(IHealth newUnit)
    {
        RegisterUnit(newUnit);
    }

    // --- M�TODO PARA CUANDO DESTRUYEN LA BASE ---
    public void NotificarBaseDestruida()
    {
        baseDestruida = true;
        // El Update lo detectar� en el siguiente frame
    }

    // --- Mantenemos este m�todo para que BuildingManager no de error, pero ya no hace falta l�gica ---
    public void VerificarDineroTrasGasto()
    {
        // El Update se encarga de esto autom�ticamente ahora.
    }

    private void TriggerGameOver()
    {
        if (gameOver) return;

        gameOver = true;

        // Reproducir sonido derrota si existe
        if (SoundColector.Instance != null) SoundColector.Instance.PlayDefeatMusic();

        // Cargar escena de Game Over (Indice 9)
        SceneManager.LoadScene(9);
    }

    // --- UTILIDADES (Se mantienen todas) ---
    public int GetActiveUnitsCount()
    {
        return allUnits.Count;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public void ResetGame()
    {
        allUnits.Clear();
        gameOver = false;
        baseDestruida = false;
    }

    public List<IHealth> GetAllUnits()
    {
        return new List<IHealth>(allUnits);
    }

    public List<T> GetUnitsByType<T>() where T : class, IHealth
    {
        List<T> result = new List<T>();
        foreach (var unit in allUnits)
        {
            if (unit is T typedUnit)
            {
                result.Add(typedUnit);
            }
        }
        return result;
    }
}