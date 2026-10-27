
using Godot;

// Компонент отвечает за создание и запуск пуль
[GlobalClass]
public partial class ShootComponent : Node
{
    [Export]
    private float _FireRate = 0.5f; // Интервал между выстрелами 

    [Export]
    private float _BulletSpeed = 10f; // Скорость полета пули

    [Export]
    private float _BulletDamage = 25f; // Наносимый урон

    [Export]
    private float _ShootRange = 1000f; // дальность стрельбы

    private float _TimeSinceLastShot = 0f;

    private Node3D _Parent;

    public Node3D target;   

    private Marker3D _Muzzel; 

    public override void _Ready()
    {
        _Parent = GetParent<Node3D>();
        // Находим маркер вылета пули внутри башни.
        _Muzzel = _Parent.GetNodeOrNull<Marker3D>("Muzzel");

        if (_Muzzel == null)
        {
            GD.PushWarning("Не найден маркер у башни!");
        }
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_Parent))
        {
            return;
        }
        // Считаем время, прошедшее после предыдущего выстрела
        _TimeSinceLastShot += (float)delta;
        if (target != null && GodotObject.IsInstanceValid(target))
        {
            Vector3 targetPosition = new Vector3(
                target.GlobalPosition.X,
                _Parent.GlobalPosition.Y,
                target.GlobalPosition.Z
            );
            
            _Parent.LookAt(targetPosition, Vector3.Up);
        }

        // Стреляем, только если есть цель и прошло нужное время
        if (GodotObject.IsInstanceValid(target) && _TimeSinceLastShot >= _FireRate)
        {
            Shoot(target);
            _TimeSinceLastShot = 0f;
        }
    }

    // Находит ближайшего противника в пределах дальности стрельбы.
    private Node3D FindClosestTarget()
    {
        string targetGroup;
            if (_Parent.IsInGroup("allies"))
            {
                targetGroup = "enemies";
            }
            else
            {
                targetGroup = "allies";
            }

        Node3D closestTarget = null;
        float closestDistance = _ShootRange;

        foreach (Node node in GetTree().GetNodesInGroup(targetGroup))
        {
            if (node is not Node3D target)
            {
                continue;
            }

            float distance = _Muzzel.GlobalPosition.DistanceTo(
                target.GlobalPosition
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        return closestTarget;
    }
    [Export]
    private PackedScene _BulletScene = GD.Load<PackedScene>("res://scenes/Bullet.tscn"); //загрузка пули

    // Создаёт пулю и задаёт ей направление движения.
    private void Shoot(Node3D target)
    {
        ShootAtPosition(target.GlobalPosition);
    }
    // Выполняет выстрел в указанную точку мира.
    public void ShootAtPosition(Vector3 targetPosition)
    {
        // Проверяем, прошло ли достаточно времени после выстрела.
        if (_TimeSinceLastShot < _FireRate)
        {
            return;
        }

        // Проверяем наличие сцены пули и маркера стрельбы.
        if (_BulletScene == null || _Muzzel == null)
        {
            return;
        }

        // Создаём экземпляр пули.
        Bullet bullet = _BulletScene.Instantiate<Bullet>();
        GetTree().CurrentScene.AddChild(bullet);
        bullet.GlobalPosition = _Muzzel.GlobalPosition;

        Vector3 direction = (targetPosition - _Muzzel.GlobalPosition).Normalized();
        bullet.Initialize(direction, _BulletSpeed, _BulletDamage, _Parent);
        _TimeSinceLastShot = 0f;
    }
}
