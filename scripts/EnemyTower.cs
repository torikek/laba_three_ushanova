using Godot;
using System;

public partial class EnemyTower : CharacterBody3D
{
    public override void _Ready()
    {
        AddToGroup("enemies");
    }
}
