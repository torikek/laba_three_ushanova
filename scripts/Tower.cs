using Godot;
using System;

public partial class Tower : CharacterBody3D
{
    public override void _Ready()
    {
        AddToGroup("allies");
    }
}
