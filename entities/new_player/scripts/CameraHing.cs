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

    private Array<RayCast3D> ray_casts = new Array<RayCast3D>();

    [Export]
    public Node3D target_node;

    public float last_target_angle = 0.0f;

    private bool _mouse_captured = false;

    private bool mouse_captured {
        get { return _mouse_captured; }
        set {
            _mouse_captured = value;

            if (value) {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            else {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        camera = GetNode<Camera3D>("Camera3D");

        ray_casts.Add(GetNode<RayCast3D>("RayCast3D"));
        ray_casts.Add(GetNode<RayCast3D>("RayCast3D2"));
        ray_casts.Add(GetNode<RayCast3D>("RayCast3D3"));
        ray_casts.Add(GetNode<RayCast3D>("RayCast3D4"));
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsClass("InputEventMouseMotion") && mouse_captured) {
            var motion = (InputEventMouseMotion)@event;

            target_dir -= motion.Relative * 0.001f;
        }
    }

    private float CalculateTargetAngle(out bool flipped) {
        var direction = ToLocal(target_node.GlobalPosition);

        var angle = (float)Math.Atan2(direction.X, direction.Z);

        if (Math.Abs(angle) <= ((float)Math.PI / 2.0f)) {
           angle = 2.0f * (float)Math.PI - angle;
           flipped = true;
        }
        else {
            angle += (float)Math.PI;
            flipped = false;
        }

        return angle;
    }

    private float CalculateTargetAngle() {
        return CalculateTargetAngle(out _);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var look_delta = Input.GetVector("look_right", "look_left", "look_down", "look_up");

        target_dir += look_delta * 0.01f;

        target_dir.Y = Math.Clamp(target_dir.Y, (float)-Math.PI / 2.5f, (float)Math.PI / 4.0f);

        if (Input.IsActionJustPressed("shield")) {
            if (target_node != null) {
                var angle = CalculateTargetAngle();

                last_target_angle = angle;

                target_dir = new Vector2(angle, 0.0f);
            }
            else {
                target_dir = Vector2.Zero;
            }
        }
        else if (Input.IsActionPressed("shield") && target_node != null) {
            bool flipped;
            var angle = CalculateTargetAngle(out flipped);

            var delta_angle = Mathf.AngleDifference(last_target_angle, angle);
            last_target_angle = angle;

            if (flipped) {
                delta_angle = -delta_angle;
            }

            target_dir = target_dir with {X = target_dir.X + delta_angle};
        }

        look_dir = look_dir with {
            X = Mathf.LerpAngle(look_dir.X, target_dir.X, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta)),
            Y = Mathf.LerpAngle(look_dir.Y, target_dir.Y, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta))
        };

        Rotation = Rotation with {X = look_dir.Y, Y = look_dir.X};

        if (Input.IsActionJustPressed("ui_cancel")) {
            mouse_captured = !mouse_captured;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Vector3 target_pos = Vector3.Back * (ray_casts[0].TargetPosition.Length() - margin);
        
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
