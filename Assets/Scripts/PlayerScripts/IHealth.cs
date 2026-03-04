// IHealth.cs
using UnityEngine;

public interface IHealth
{
    // Propiedades
    Transform transform { get; }
    bool IsDead { get; }

    // Métodos esenciales
    void TakeDamage(int damage);
    void Die();

    // Métodos de estado
    int GetCurrentHealth();
    int GetMaxHealth();
    bool IsFullHealth();

    // Métodos de curación
    void Heal(int amount);

    // Métodos UI
    void SetHealthBarVisible(bool visible);

    // Métodos para escudo (opcional - pueden lanzar excepción si no se implementan)
    int GetCurrentShield();
    int GetMaxShield();
    bool IsFullShield();
    void RepairShield(int amount);
}