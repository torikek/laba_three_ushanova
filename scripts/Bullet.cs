using Godot;

// Пуля отвечает за полёт и время жизни; попадания обрабатывает компонент урона.
[GlobalClass]
public partial class Bullet : Node3D
{
    [Export]
    private float _lifetime = 5f;

    private Vector3 _direction = Vector3.Zero;
    private float _speed;
    private float _timeAlive;
    private DamagingComponent _damagingComponent;

    public override void _Ready()
    {
        _damagingComponent = GetNodeOrNull<DamagingComponent>("DamagingComponent");
        if (_damagingComponent == null)
        {
            GD.PushError("Bullet: DamagingComponent не найден!");
        }
    }

    // ShootComponent вызывает этот метод после добавления пули в дерево сцены.
    public void Initialize(Vector3 direction, float speed, float damage, Node3D shooter)
    {
        _direction = direction.Normalized();
        _speed = speed;

        string targetGroup = "";
        if (GodotObject.IsInstanceValid(shooter))
        {
            if (shooter.IsInGroup("allies"))
            {
                targetGroup = "enemies";
            }
            else if (shooter.IsInGroup("enemies"))
            {
                targetGroup = "allies";
            }
            else
            {
                GD.PushWarning($"У {shooter.Name} нет группы команды!");
            }
        }

        if (_damagingComponent != null)
        {
            _damagingComponent.SetDamage(damage);
            _damagingComponent.SetTargetGroup(targetGroup);
            _damagingComponent.SetSource(this, shooter);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _timeAlive += (float)delta;
        if (_timeAlive >= _lifetime)
        {
            QueueFree();
            return;
        }

        GlobalPosition += _direction * _speed * (float)delta;
    }
}
