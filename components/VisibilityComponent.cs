
using Godot;
using System;


[GlobalClass]
public partial class VisibilityComponent : Area3D
{
    [Export]
    private CollisionShape3D _DetectionShape;

    [Export]
    public MoveComponent MoveComponent;
    [Export]
    public ShootComponent ShootComponent;

    private Godot.Collections.Array<Node3D> _DetectedBodies =
        new Godot.Collections.Array<Node3D>();

    private Node3D _Target;

    public override void _Ready()
    {
        // Подключаем сигналы обнаружения и выхода объектов
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        // Учитываем объекты, которые уже находятся внутри зоны
        foreach (Node3D body in GetOverlappingBodies())
        {
            OnBodyEntered(body);
        }
    }

    // Проверяет, является ли вошедшее тело противником
    private void OnBodyEntered(Node3D body)
    {
        Node parent = GetParent();

        bool parentIsEnemy = parent.IsInGroup("enemies");
        bool parentIsAlly = parent.IsInGroup("allies");

        bool isOpponent =
            (parentIsEnemy && body.IsInGroup("allies")) ||
            (parentIsAlly && body.IsInGroup("enemies"));

        if (isOpponent && !_DetectedBodies.Contains(body))
        {
            _DetectedBodies.Add(body);
        }
    }

    // Удаляем тело из списка, когда оно выходит из зоны
    private void OnBodyExited(Node3D body)
    {
        _DetectedBodies.Remove(body);
    }

    public override void _Process(double delta)
    {
        Node3D closestBody = null;
        float closestDistance = float.MaxValue;

        // Находим ближайшее обнаруженное тело
        foreach (Node3D body in _DetectedBodies)
        {
            if (!GodotObject.IsInstanceValid(body))
            {
                continue;
            }

            float distance = GetParent<CharacterBody3D>().GlobalPosition.DistanceTo(
                body.GlobalPosition
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBody = body;
            }
        }

        _Target = closestBody;

        // Передаём цель компоненту движения
        if (MoveComponent != null)
        {
            MoveComponent.SetTarget(_Target);
        }
        if (ShootComponent != null)
        {
            ShootComponent.target = _Target;
        }
    }
}
