
using Godot;

// Пуля отвечает за движение и обнаружение столкновений.
// Сам урон наносит отдельный DamagingComponent.
[GlobalClass]
public partial class Bullet : Area3D
{
    // Максимальное время жизни пули.
    [Export]
    private float _lifetime = 5f;

    // Направление полета.
    private Vector3 _direction = Vector3.Zero;

    // Скорость полета.
    private float _speed = 10f;

    // Сколько времени существует пуля.
    private float _timeAlive = 0f;

    // Объект, выпустивший пулю.
    private Node3D _owner;

    // Группа объектов, которые можно повреждать.
    private string _targetGroup = "";

    // Компонент нанесения урона.
    private DamagingComponent _damagingComponent;

    public override void _Ready()
    {
        // Ищем дочерний компонент урона.
        _damagingComponent =
            GetNodeOrNull<DamagingComponent>(
                "DamagingComponent"
            );

        // Сообщаем об ошибке настройки сцены.
        if (_damagingComponent == null)
        {
            GD.PushError(
                "Bullet: DamagingComponent не найден!"
            );
        }

        // Включаем регистрацию столкновений с телами.
        Monitoring = true;

        // Подключаем обработчик столкновения.
        BodyEntered += OnBodyEntered;
    }

    // Инициализация параметров новой пули.
    public void Initialize(
        Vector3 direction,
        float speed,
        float damage,
        Node3D shooter)
    {
        // Сохраняем нормализованное направление.
        _direction = direction.Normalized();

        // Устанавливаем скорость.
        _speed = speed;

        // Запоминаем стрелявшего.
        _owner = shooter;

        // Определяем команду стрелявшего.
        if (shooter.IsInGroup("allies"))
        {
            // Союзные пули повреждают врагов.
            _targetGroup = "enemies";
        }
        else if (shooter.IsInGroup("enemies"))
        {
            // Вражеские пули повреждают союзников.
            _targetGroup = "allies";
        }
        else
        {
            GD.PushWarning(
                $"У {shooter.Name} нет группы команды!"
            );
        }

        // Настраиваем отдельный компонент урона.
        if (_damagingComponent != null)
        {
            // Передаем величину урона.
            _damagingComponent.SetDamage(damage);

            // Передаем группу разрешенных целей.
            _damagingComponent.SetTargetGroup(
                _targetGroup
            );
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Увеличиваем время существования.
        _timeAlive += (float)delta;

        // Удаляем пулю после окончания времени жизни.
        if (_timeAlive >= _lifetime)
        {
            QueueFree();
            return;
        }

        // Рассчитываем перемещение за физический кадр.
        Vector3 movement =
            _direction * _speed * (float)delta;

        // Перемещаем пулю в мировых координатах.
        GlobalPosition += movement;
    }

    // Обработчик попадания в физическое тело.
    private void OnBodyEntered(Node3D body)
    {
        // Не обрабатываем столкновение со стрелявшим.
        if (body == _owner)
        {
            return;
        }

        // Проверяем принадлежность цели к нужной команде.
        if (!body.IsInGroup(_targetGroup))
        {
            return;
        }

        // Проверяем наличие компонента урона.
        if (_damagingComponent == null)
        {
            return;
        }

        // Поручаем компоненту нанести урон.
        bool damageApplied =
            _damagingComponent.ApplyDamage(body);

        // Если урон прошел, выводим сообщение.
        if (damageApplied)
        {
            GD.Print($"Пуля попала в {body.Name}");

            // Удаляем пулю после попадания.
            QueueFree();
        }
    }
}
