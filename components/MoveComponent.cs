using Godot;
using System;


[GlobalClass]
public partial class MoveComponent : Node
{
    protected CharacterBody3D parentNode;
    [Export]
    protected float Speed {set;get;} = 5f;
    [Export]
    protected float StopRadius {set;get;} = 100f;
    protected Node3D Target {set;get;}

    public override void _Ready()
    {
        if (GetParent() is CharacterBody3D parentNode) this.parentNode = parentNode;
    }

    public void SetTarget(Node3D target)
    {
        Target = target;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (parentNode == null)
        {
            GD.Print("MoveComponent not attached to CharacterBody3D");
            return;
        }
        else Move(parentNode);
    }
    protected void Move(CharacterBody3D parentNode)
    {
        Vector3 positionDifference;
        if (Target == null) //если цели нет движемся к центру поля
        {
            //GD.Print("Target isn't set? moving to screen center");
            positionDifference = (Vector3.Zero - parentNode.GlobalPosition);
        }
        else 
        {
            positionDifference = (Target.GlobalPosition - parentNode.GlobalPosition);
        }

        if (positionDifference.Length() > StopRadius)
        {
            Vector3 direction = positionDifference.Normalized();
            Vector3 velocity = new Vector3(
            direction.X * Speed,
            0f, 
            direction.Z * Speed
            );
            
            parentNode.Velocity = velocity;
            parentNode.MoveAndSlide();
        }

    }

}
