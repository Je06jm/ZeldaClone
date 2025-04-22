using Godot;
using System;

public partial class DayNightCycle : Node
{
    [Export]
    public DirectionalLight3D sun = null;

    [Export]
    public WorldEnvironment environment = null;

    [Export]
    public float daytime_in_seconds = 0.0f;

    [Export]
    public float total_seconds_in_day = 100.0f;

    [Export]
    public Vector3 rotation_axis = Vector3.Right;

    private void UpdateSun() {
        if (sun == null) {
            return;
        }

        if (environment == null) {
            return;
        }


    }
}
