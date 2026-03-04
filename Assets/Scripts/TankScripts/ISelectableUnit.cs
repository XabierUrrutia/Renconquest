using UnityEngine;

public interface ISelectableUnit
{
    void Seleccionar();
    void Deseleccionar();
    void MoverADestino(Vector3 destino);
    GameObject gameObject { get; } // Para poder acceder al transform/gameobject
    Transform transform { get; }
    string tag { get; }
}