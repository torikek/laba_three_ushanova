using Godot;

// Хранит здоровье объекта и сообщает о его смерти.
[GlobalClass]
public partial class HealthComponent : Node
{
    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public float MaxHealth { get; set; } = 100f;

    public float CurrentHealth { get; private set; }

    public bool IsDead { get; private set; }

    private Node3D _parent;

    public override void _Ready()
    {
        _parent = GetParent<Node3D>();

        MaxHealth = Mathf.Max(1f, MaxHealth);
        CurrentHealth = MaxHealth;
    }

    // Повторные попадания по уже погибшему объекту игнорируются.
    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(
            CurrentHealth + amount,
            MaxHealth
        );
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        // Сначала уведомляем, затем удаляем объект.
        EmitSignal(SignalName.Died);

        _parent.QueueFree();
    }
}