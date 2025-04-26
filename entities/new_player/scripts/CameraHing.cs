using Godot;
using Godot.Collections;
using System;

public partial class CameraHing : Node3D
{
    private Camera3D camera;

    private Vector2 look_dir = Vector2.Zero;
    private Vector2 target_dir = Vector2.Zero;

    private const float LOOK_CHANGE_ALPHA = 0.00001f;
    private const float SPRING_CHANGE_ALPHA = 0.0001f;

    [Export]
    private float margin = 0.5f;

    [Export]
    public Array<RayCast3D> ray_casts;

    [Export]
    public Node3D target_node;

    private float target_distance;

    public override void _Ready()
    {
        base._Ready();

        camera = GetNode<Camera3D>("Camera3D");

        target_distance = 0.0f;

        foreach (var ray in ray_casts) {
            target_distance += ray.TargetPosition.Length() - margin;
        }

        target_distance /= ray_casts.Count;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var look_delta = Input.GetVector("look_right", "look_left", "look_down", "look_up");

        target_dir += look_delta * 0.01f;

        target_dir.Y = Math.Clamp(target_dir.Y, (float)-Math.PI / 2.5f, (float)Math.PI / 4.0f);

        if (Input.IsActionJustPressed("shield")) {
            target_dir = Vector2.Zero;
        }

        if (target_node != null && Input.IsActionPressed("shield")) {
            var direction = ToLocal(target_node.GlobalPosition);

            var angle = (float)Math.Atan2(direction.X, direction.Z) + (float)Math.PI;

            look_dir = look_dir with {
                X = Mathf.LerpAngle(look_dir.X, target_dir.X + angle, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta)),
                Y = Mathf.LerpAngle(look_dir.Y, target_dir.Y, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta))
            };
        }
        else {
            look_dir = look_dir with {
                X = Mathf.LerpAngle(look_dir.X, target_dir.X, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta)),
                Y = Mathf.LerpAngle(look_dir.Y, target_dir.Y, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta))
            };
        }

        Rotation = Rotation with {X = look_dir.Y, Y = look_dir.X};
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Vector3 target_pos = Vector3.Back * target_distance;
        
        foreach (var ray in ray_casts) {
            if (ray.IsColliding()) {
                var point = ray.GetCollisionPoint();
                point = ToLocal(point);

                var length = point.Length() - margin;

                var target = Vector3.Back * length;

                if (target.LengthSquared() < target_pos.LengthSquared()) {
                    target_pos = target;
                }
            }
        }

        camera.Position = camera.Position.Lerp(target_pos, 1.0f - (float)Math.Pow(SPRING_CHANGE_ALPHA, delta));
    }


}
