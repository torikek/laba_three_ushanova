using Godot;

// создаёт взрыв на враге и уничтожает его после задержки
[GlobalClass]
public partial class BoomComponent : DamagingComponent
{
    [Export]
    private PackedScene _boomScene = GD.Load<PackedScene>("res://scenes/Boom.tscn");

    [Export(PropertyHint.Range, "0.1,10,0.1")] // мин длительность, макс длительность, шаг
    private float _boomDuration = 1f; //длительность взрыва в секундах

    private bool _hasHit;

    // запускает взрыв только при первом попадании по живому врагу
    public override bool ApplyDamage(Node target)
    {
        if (_hasHit || !TryGetTargetHealth(target, out HealthComponent health)
            || !target.IsInGroup("enemies")
            || health.IsDead || target is not Node3D body) 
        {
            return false;
        }

        if (_boomScene == null)
        {
            GD.PushError("BoomComponent: назначь сцену Boom в инспекторе");
            return false;
        }

        _hasHit = true;

        // дочерний взрыв следует за врагом до его уничтожения
        Node3D boom = _boomScene.Instantiate<Node3D>();
        body.AddChild(boom);
        boom.Position = Vector3.Zero;

        // таймер дерева сцены продолжает работать после удаления снаряда
        SceneTreeTimer timer = GetTree().CreateTimer(Mathf.Max(0.1f, _boomDuration));
        timer.Timeout += () => FinishExplosion(target, health, boom);

        // удаляет попавший снаряд без немедленного урона врагу
        CompleteHit();
        return true;
    }

    // удаляет взрыв и наносит смертельный урон через компонент здоровья
    private static void FinishExplosion(Node target, HealthComponent health, Node3D boom)
    {
        if (GodotObject.IsInstanceValid(boom) && !boom.IsQueuedForDeletion())
        {
            boom.QueueFree();
        }

        // враг мог погибнуть от другой атаки до завершения таймера
        if (!GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(health) || health.IsQueuedForDeletion()
            || health.IsDead)
        {
            return;
        }

        health.TakeDamage(health.CurrentHealth);
    }
}
