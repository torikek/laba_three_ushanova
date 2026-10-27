
using Godot;
using System;

[GlobalClass]
public partial class DamagingComponent : Area3D
{
    // Количество урона, наносимого за одно попадание.
    [Export]
    private float _Damage = 10f;

    // Группы объектов, которым разрешено наносить урон.
    [Export]
    private Godot.Collections.Array<string> _TargetGroups = new();

    // Если true, компонент сам наносит урон при столкновении.
    // Для пули оставляем false, так как попадание обрабатывает Bullet.
    [Export]
    private bool _DamageOnContact = false;

    // Список идентификаторов объектов, уже получивших контактный урон.
    private Godot.Collections.Array<ulong> _DamagedTargets = new();

    public override void _Ready()
    {
        // Подписываемся на столкновения только для контактной атаки.
        if (_DamageOnContact)
        {
            BodyEntered += OnBodyEntered;
        }
    }

    // Обрабатывает контактный урон, например атаку Crawler.
    private void OnBodyEntered(Node3D body)
    {
        ulong targetId = body.GetInstanceId();

        // Не наносим урон повторно одной и той же цели.
        if (_DamagedTargets.Contains(targetId))
        {
            return;
        }

        if (ApplyDamage(body))
        {
            _DamagedTargets.Add(targetId);
        }
    }

    // Устанавливает урон, переданный пуле из ShootComponent.
    public void SetDamage(float damage)
    {
        _Damage = damage;
    }

    // Настраивает группу целей, которым компонент может наносить урон.
    public void SetTargetGroup(string group)
    {
        _TargetGroups.Clear();
        _TargetGroups.Add(group);
    }

    // Наносит урон объекту, если он входит в разрешённую группу.
    public bool ApplyDamage(Node target)
    {
        if (target == null)
        {
            return false;
        }

        // Проверяем принадлежность цели к одной из нужных групп.
        bool allowedTarget = false;

        foreach (string group in _TargetGroups)
        {
            if (target.IsInGroup(group))
            {
                allowedTarget = true;
                break;
            }
        }

        if (!allowedTarget)
        {
            return false;
        }

        // Ищем HealthComponent среди непосредственных дочерних узлов.
        HealthComponent health =
            target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (health == null)
        {
            GD.PushWarning(
                $"У объекта {target.Name} не найден HealthComponent."
            );

            return false;
        }

        // Передаём компоненту здоровья величину урона.
        health.TakeDamage(_Damage);

        return true;
    }
}
