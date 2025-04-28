using Godot;
using Godot.Collections;
using System;

public partial class CameraHing : Node3D
{
    private Camera3D camera;

    private Vector2 look_dir = Vector2.Zero;
    private Vector2 target_dir = Vector2.Zero;

    public float direction {
        get { return look_dir.X; }
    }

    private const float LOOK_CHANGE_ALPHA = 0.00001f;
    private const float SPRING_CHANGE_ALPHA = 0.0001f;
    private const float VERTICAL_CHANGE_ALPHA = 0.00001f;
    private const float LOCK_LOOK_ANGLE = -0.3f;
    private const float TARGET_LOCK_LOOK_ANGLE = -0.6f;

    [Export]
    private float margin = 0.5f;

    private Array<RayCast3D> ray_casts = new Array<RayCast3D>();

    [Export]
    public Node3D target_node;

    [Export]
    private Character character = null;

    private float last_target_angle = 0.0f;
    private float last_target_vertical_angle = 0.0f;

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

    private Sprite2D target_indicator;

    public override void _Ready()
    {
        base._Ready();

        camera = GetNode<Camera3D>("Camera3D");

        ray_casts.Add(GetNode<RayCast3D>("RayCast3D"));
        ray_casts.Add(GetNode<RayCast3D>("RayCast3D2"));
        ray_casts.Add(GetNode<RayCast3D>("RayCast3D3"));
        ray_casts.Add(GetNode<RayCast3D>("RayCast3D4"));

        target_indicator = GetNode<Sprite2D>("TargetIndicator");
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsClass("InputEventMouseMotion") && mouse_captured) {
            var motion = (InputEventMouseMotion)@event;

            target_dir -= motion.Relative * 0.001f * GameSettings.mouse_look_speed;
        }
    }

    private float CalculateTargetAngle() {
        var global_pos_2d = new Vector2(GlobalPosition.X, GlobalPosition.Z);
        var target_pos_2d = new Vector2(target_node.GlobalPosition.X, target_node.GlobalPosition.Z);

        return (float)Math.PI - global_pos_2d.AngleToPoint(target_pos_2d) + (float)Math.PI / 2.0f;
    }

    private float CalculateTargetVerticalAngle() {
        var global_pos_2d = new Vector2(GlobalPosition.X, GlobalPosition.Z);
        var target_pos_2d = new Vector2(target_node.GlobalPosition.X, target_node.GlobalPosition.Z);

        var dist = global_pos_2d.DistanceTo(target_pos_2d);
        var height = GlobalPosition.Y - target_node.GlobalPosition.Y;

        return -Vector2.Zero.AngleToPoint(new Vector2(dist, height));
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

                var virt_angle = CalculateTargetVerticalAngle();

                last_target_vertical_angle = virt_angle;

                target_dir = new Vector2(angle, virt_angle + TARGET_LOCK_LOOK_ANGLE);
            }
            else {
                target_dir = new Vector2(character.target_direction, LOCK_LOOK_ANGLE);
            }
        }
        else if (Input.IsActionPressed("shield") && target_node != null) {
            var angle = CalculateTargetAngle();

            var delta_angle = Mathf.AngleDifference(last_target_angle, angle);
            last_target_angle = angle;

            var virt_angle = CalculateTargetVerticalAngle();

            var delta_virt_angle = Mathf.AngleDifference(last_target_vertical_angle, virt_angle);
            last_target_vertical_angle = virt_angle;

            target_dir = target_dir with {X = target_dir.X + delta_angle, Y = target_dir.Y + delta_virt_angle};
        }

        look_dir = look_dir with {
            X = Mathf.LerpAngle(look_dir.X, target_dir.X, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta)),
            Y = Mathf.LerpAngle(look_dir.Y, target_dir.Y, 1.0f - (float)Math.Pow(LOOK_CHANGE_ALPHA, delta))
        };

        Rotation = Rotation with {X = look_dir.Y, Y = look_dir.X};

        if (Input.IsActionJustPressed("ui_cancel")) {
            mouse_captured = !mouse_captured;
        }

        if (target_node != null && Input.IsActionPressed("shield")) {
            target_indicator.Visible = true;
            var look_target = (LookTarget)target_node;
            var pos3d = look_target.target_indicator_position.GlobalPosition;
            var pos2d = camera.UnprojectPosition(pos3d);
            target_indicator.GlobalPosition = pos2d;
        }
        else {
            target_indicator.Visible = false;
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

        Position = Position with {
            X = character.Position.X,
            Z = character.Position.Z,
            Y = Mathf.Lerp(Position.Y, character.Position.Y, 1.0f - (float)Math.Pow(VERTICAL_CHANGE_ALPHA, delta))
        };
    }
}
