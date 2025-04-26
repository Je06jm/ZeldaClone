using Godot;
using System;

public partial class CsgSphere3d : CsgSphere3D
{
    private float time = 0.0f;

    public override void _Process(double delta)
    {
        base._Process(delta);

        time += (float)delta;

        Position = Position with {X = (float)Math.Sin(time)};
    }
}
