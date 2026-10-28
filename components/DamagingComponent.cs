using Godot;

// Общие правила попадания и урона для снарядов, контактных врагов и эффектов.
[GlobalClass]
public partial class DamagingComponent : Area3D
{
    [Export]
    private float _damage = 10f;

    [Export]
    private Godot.Collections.Array<string> _targetGroups = new();

    [Export]
    private bool _damageOnContact = false;

    [Export]
    private bool _destroySourceAfterHit = true;

    private Node _source;
    private Node _instigator;
    private bool _hasTriggered;

    // Наследники получают величину урона без прямого доступа к полю.
    protected float Damage => _damage;

    public override void _Ready()
    {
        _source ??= GetParent();
        Monitoring = _damageOnContact;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_damageOnContact)
        {
            ApplyDamage(body);
        }
    }

    public void SetDamage(float damage)
    {
        _damage = Mathf.Max(0f, damage);
    }

    public void SetTargetGroup(string group)
    {
        _targetGroups.Clear();
        if (!string.IsNullOrEmpty(group))
        {
            _targetGroups.Add(group);
        }
    }

    // source удаляется после попадания, instigator — стрелявший, которого игнорируем.
    public void SetSource(Node source, Node instigator = null)
    {
        _source = source;
        _instigator = instigator;
    }

    // Виртуальный метод позволяет наследникам реализовать взрыв или иной эффект.
    public virtual bool ApplyDamage(Node target)
    {
        if (!TryGetTargetHealth(target, out HealthComponent health))
        {
            return false;
        }

        // QueueFree отложен до конца кадра: блокируем повторный урон сразу.
        if (_destroySourceAfterHit)
        {
            _hasTriggered = true;
        }

        health.TakeDamage(Damage);
        CompleteHit();
        return true;
    }

    // Общая проверка доступна новым компонентам урона.
    protected bool CanApplyDamage(Node target)
    {
        if (_hasTriggered || IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion()
            || target == this || target == _source || target == _instigator)
        {
            return false;
        }

        if (GodotObject.IsInstanceValid(_source) && _source.IsQueuedForDeletion())
        {
            return false;
        }

        foreach (string group in _targetGroups)
        {
            if (!string.IsNullOrEmpty(group) && target.IsInGroup(group))
            {
                return true;
            }
        }

        return false;
    }

    protected bool TryGetTargetHealth(Node target, out HealthComponent health)
    {
        health = null;
        if (!CanApplyDamage(target))
        {
            return false;
        }

        health = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        return GodotObject.IsInstanceValid(health) && !health.IsQueuedForDeletion();
    }

    // Наследники завершают одноразовый эффект после выполнения собственных атак.
    protected virtual void CompleteHit()
    {
        if (!_destroySourceAfterHit)
        {
            return;
        }

        _hasTriggered = true;
        if (GodotObject.IsInstanceValid(_source))
        {
            _source.QueueFree();
        }
        else
        {
            QueueFree();
        }
    }
}
