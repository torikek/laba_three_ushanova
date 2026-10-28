using Godot;

// Движется к обнаруженной цели, а при её отсутствии — к ядру базы.
[GlobalClass]
public partial class MoveComponent : Node
{
    [Export]
    public float Speed { get; set; } = 5f;

    [Export]
    public float StopRadius { get; set; } = 0.5f;

    private CharacterBody3D _body;
    private Node3D _target;
    private Node3D _core;

    public override void _Ready()
    {
        _body = GetParent() as CharacterBody3D;
        if (_body == null)
        {
            GD.PushError("MoveComponent должен быть дочерним узлом CharacterBody3D.");
        }
    }

    public void SetTarget(Node3D target)
    {
        _target = target;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAvailable(_body))
        {
            return;
        }

        Node3D destination = GetDestination();
        if (destination == null)
        {
            _body.Velocity = Vector3.Zero;
            return;
        }

        Vector3 offset = destination.GlobalPosition - _body.GlobalPosition;
        offset.Y = 0f;

        float stopRadius = Mathf.Max(0f, StopRadius);
        if (offset.LengthSquared() <= stopRadius * stopRadius)
        {
            _body.Velocity = Vector3.Zero;
            return;
        }

        _body.Velocity = offset.Normalized() * Mathf.Max(0f, Speed);
        _body.MoveAndSlide();
    }

    private Node3D GetDestination()
    {
        if (IsAvailable(_target))
        {
            return _target;
        }

        _target = null;

        // Запасная цель нужна только врагам; союзники без цели стоят на месте.
        if (!_body.IsInGroup("enemies"))
        {
            return null;
        }

        if (!IsAvailable(_core))
        {
            _core = GetTree().GetFirstNodeInGroup("core") as Node3D;
        }

        return IsAvailable(_core) ? _core : null;
    }

    private bool IsAvailable(Node3D node)
    {
        return GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }
}
