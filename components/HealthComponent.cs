using Godot;
using System;

[GlobalClass]
public partial class HealthComponent : Node
{
    private Node3D parentNode;
    [Export]
    protected float MaxHealth = 100f;
    protected float CurrentHealth {get;set;}

    public override void _Ready()
    {
        parentNode = GetParent<Node3D>();
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float damage) // Получение урона
    {
        CurrentHealth -= damage;
        if (CurrentHealth <=0f)
        {
            Die();
        }
    }
    // Восстанавливает здоровье, но не выше максимального значения
    public void Heal(float amount)
    {
        if (amount <= 0f || CurrentHealth <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }
    private void Die() //удаляем объект после 0 здоровья
    {
        GD.Print("die!");
        parentNode.QueueFree();
    }
}
