using Godot;
using System;

[Tool]
[GlobalClass]
public partial class LookTarget : Area3D
{
    [Export]
    public Node3D target_indicator_position;

    private CollisionShape3D collision_shape;
    private VisibleOnScreenNotifier3D on_screen_notifier;

    private const float radius = 0.25f;

    public bool on_screen {
        get {
            return on_screen_notifier.IsOnScreen();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        collision_shape = new CollisionShape3D();
        var sphere = new SphereShape3D();
        sphere.Radius = radius;
        collision_shape.Shape = sphere;

        AddChild(collision_shape);

        var half = radius / 2.0f;

        on_screen_notifier = new VisibleOnScreenNotifier3D();
        on_screen_notifier.Aabb = new Aabb(
            new Vector3(-half, -half, -half),
            new Vector3(radius, radius, radius)
        );

        AddChild(on_screen_notifier);

        CollisionMask = 0;
        CollisionLayer = 16; // Layer 5
    }
}
