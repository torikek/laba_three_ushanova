using Godot;
using System;

public partial class Player : CharacterBody3D
{
    [Export]
    private float _Speed = 5f;

    private ShootComponent _ShootComponent;
    private Camera3D _Camera;

    public override void _Ready()
    {
        _ShootComponent = GetNodeOrNull<ShootComponent>("ShootComponent");

        if (_ShootComponent == null)
        {
            GD.Print("У игрока нет шут компоненты");
        }
        _Camera = GetViewport().GetCamera3D(); // Получаем активную 3D-камеру
        AddToGroup("allies"); // Добавляем игрока в группу союзников

    }

    public override void _PhysicsProcess(double delta)
    {
        MovePlayer();
        MoveAndSlide();
    }
    public override void _Process(double delta)
    {
        // Поворачиваем игрока к курсору.
        RotateTowardsMouse();

        // Стреляем при нажатии левой кнопки мыши.
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            ShootAtMouse();
        }
    }

    private void MovePlayer()
    {
        Vector2 input = Input.GetVector(
            "move_right",
            "move_left",
            "move_forward",
            "move_back"
        );
        // Переводим ввод в направление на плоскости XZ.
        Vector3 direction = new Vector3(input.X, 0f, input.Y);

        Velocity = direction.Normalized() * _Speed;
    }
    
    private bool TryGetMousePosition(out Vector3 mousePosition)
    {
        mousePosition = Vector3.Zero;

        if (_Camera == null)
        {
            _Camera = GetViewport().GetCamera3D();
        }

        if (_Camera == null)
        {
            return false;
        }

        Vector2 mouseScreenPosition =
            GetViewport().GetMousePosition();

        Vector3 rayOrigin =
            _Camera.ProjectRayOrigin(mouseScreenPosition);

        Vector3 rayDirection =
            _Camera.ProjectRayNormal(mouseScreenPosition);

        // Игровая плоскость проходит через Y = 0.
        Plane groundPlane = new Plane(Vector3.Up, 0f);

        // Находим пересечение луча мыши с горизонтальной плоскостью.
        Vector3? intersection =
            groundPlane.IntersectsRay(rayOrigin, rayDirection);

        // Если пересечения нет, возвращаем false.
        if (!intersection.HasValue)
        {
            return false;
        }

        // Получаем координаты пересечения.
        mousePosition = intersection.Value;

        // Сообщаем, что позиция найдена.
        return true;
    }
    // Разворачиваем игрока в сторону курсора.
    private void RotateTowardsMouse()
    {
        if (!TryGetMousePosition(out Vector3 mousePosition))
        {
            return;
        }

        Vector3 direction = mousePosition - GlobalPosition;

        // Игнорируем разницу высоты.
        direction.Y = 0f;

        if (direction.LengthSquared() < 0.001f)
        {
            return;
        }

        // В Godot 3D обычно передняя сторона модели направлена вдоль -Z.
        LookAt(
            new Vector3(
                mousePosition.X,
                GlobalPosition.Y,
                mousePosition.Z
            ),
            Vector3.Up
        );
    }

    // Стрельба в направлении курсора.
    private void ShootAtMouse()
    {
        if (_ShootComponent == null)
        {
            return;
        }

        if (!TryGetMousePosition(out Vector3 mousePosition))
        {
            return;
        }

        _ShootComponent.ShootAtPosition(mousePosition);
    }
}
