using Godot;
using System;
using System.Collections.Generic;

// временно останавливает движение и стрельбу врага
[GlobalClass]
public partial class FreezeComponent : DamagingComponent
{
    [Export]
    private float _freezeDuration = 2f; // длительность заморозки в секундах

    // хранит заморозки независимо от удаляемых снарядов
    private static readonly Dictionary<Node, FrozenState> _frozenTargets = new();

    // хранит компоненты врага и их исходные режимы
    private sealed class FrozenState
    {
        public MoveComponent Movement;
        public ShootComponent Shooting;
        public ProcessModeEnum MovementMode;
        public ProcessModeEnum ShootingMode;
        public Timer Timer;
        public Action ExitHandler;
    }

    // наносит урон и замораживает выжившего врага
    public override bool ApplyDamage(Node target)
    {
        // проверяет допустимость цели и наличие здоровья
        if (!TryGetTargetHealth(target, out HealthComponent health)
            || health.IsDead)
        {
            return false;
        }

        // вызывает общую логику урона и удаления снаряда
        bool applied = base.ApplyDamage(target);

        if (applied && !health.IsDead && !target.IsQueuedForDeletion()
            && target.IsInGroup("enemies") && _freezeDuration > 0f)
        {
            FreezeTarget(target, _freezeDuration);
        }

        return applied;
    }

    // отключает движение и стрельбу на заданное время
    private static void FreezeTarget(Node target, float duration)
    {
        // повторное попадание обновляет длительность заморозки
        if (_frozenTargets.TryGetValue(target, out FrozenState existing))
        {
            existing.Timer.Start(duration);
            return;
        }

        // находит компоненты врага и создаёт одноразовый таймер
        var state = new FrozenState
        {
            Movement = target.GetNodeOrNull<MoveComponent>("MoveComponent"),
            Shooting = target.GetNodeOrNull<ShootComponent>("ShootComponent"),
            Timer = new Timer { OneShot = true }
        };

        // сохраняет режим движения перед отключением
        if (GodotObject.IsInstanceValid(state.Movement))
        {
            state.MovementMode = state.Movement.ProcessMode;
            state.Movement.ProcessMode = ProcessModeEnum.Disabled;
        }

        // сохраняет режим стрельбы перед отключением
        if (GodotObject.IsInstanceValid(state.Shooting))
        {
            state.ShootingMode = state.Shooting.ProcessMode;
            state.Shooting.ProcessMode = ProcessModeEnum.Disabled;
        }

        // обнуляет текущую скорость врага
        if (target is CharacterBody3D body)
        {
            body.Velocity = Vector3.Zero;
        }

        _frozenTargets.Add(target, state);

        // таймер на враге работает даже после удаления пули
        state.Timer.Timeout += () => RestoreTarget(target);
        state.ExitHandler = () => RestoreTarget(target);
        target.TreeExiting += state.ExitHandler;
        target.AddChild(state.Timer);
        state.Timer.Start(duration);
    }

    // восстанавливает режимы при разморозке или выходе врага со сцены
    private static void RestoreTarget(Node target)
    {
        if (!_frozenTargets.TryGetValue(target, out FrozenState state))
        {
            return;
        }

        // удаляет запись о заморозке и отключает обработчик выхода
        _frozenTargets.Remove(target);

        if (GodotObject.IsInstanceValid(target))
        {
            target.TreeExiting -= state.ExitHandler;
        }

        if (GodotObject.IsInstanceValid(state.Movement))
        {
            state.Movement.ProcessMode = state.MovementMode;
        }

        if (GodotObject.IsInstanceValid(state.Shooting))
        {
            state.Shooting.ProcessMode = state.ShootingMode;
        }

        // останавливает и удаляет завершённый таймер
        if (GodotObject.IsInstanceValid(state.Timer))
        {
            state.Timer.Stop();
            state.Timer.QueueFree();
        }
    }
}
