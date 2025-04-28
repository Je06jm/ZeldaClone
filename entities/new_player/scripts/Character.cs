using Godot;
using System;

public partial class Character : CharacterBody3D
{
    private enum State {
        Idle,
        Walking,
        Sprinting,
        TiredWalking
    };

    private State state = State.Idle;

    private float _direction = 0.0f;

    public float direction {
        get { return _direction; }
        private set { _direction = value; }
    }

    private float _target_direction = 0.0f;

    public float target_direction {
        get { return _target_direction; }
        private set { _target_direction = value; }
    }

    private const float TURN_ALPHA = 0.001f;

    [Export]
    CameraHing hing = null;

    [Export]
    private Node3D target_node = null;

    private void UpdateState(Vector2 movement) {
        var is_moving = movement != Vector2.Zero;

        switch (state) {
            case State.Idle:
                if (is_moving) {
                    state = State.Walking;
                }
                break;

            case State.Walking:
                if (!is_moving) {
                    state = State.Idle;
                }
                break;

            default:
                break;
        }
    }

    private void UpdateDirection(Vector2 movement, float delta) {
        if (target_node != null && Input.IsActionPressed("shield")) {
            var current_pos = new Vector2(GlobalPosition.X, GlobalPosition.Z);
            var target_pos = new Vector2(target_node.GlobalPosition.X, target_node.GlobalPosition.Z);

            target_direction = current_pos.AngleToPoint(target_pos);
            target_direction = -target_direction - (float)Math.PI / 2.0f;
        }
        else if (movement != Vector2.Zero && !Input.IsActionPressed("shield")) {
            var move_direction = movement.Angle();
            move_direction = -move_direction - (float)Math.PI / 2.0f;

            target_direction = move_direction;

            target_direction += hing.direction;
        }

        direction = Mathf.LerpAngle(direction, target_direction, 1.0f - (float)Math.Pow(TURN_ALPHA, delta));
    }

    private void UpdateMovement(Vector2 movement, float delta) {
        Velocity += Vector3.Down * GameSettings.gravity * delta;

        var saved_grav = Velocity.Y;

        var forward = -Basis.Z;
        var right = Basis.X;

        switch (state) {
            case State.Idle:
                Velocity = Vector3.Zero;
                break;

            case State.Walking:
                if (Input.IsActionPressed("shield")) {
                    var move = forward * movement.Dot(Vector2.Up);
                    move += right * movement.Dot(Vector2.Right);
                    Velocity = move * movement.Length() * 4.5f;
                }
                else {
                    Velocity = forward * movement.Length() * 4.5f;
                }
                break;

            default:
                break;
        }

        if (Input.IsActionJustPressed("jump")) {
            saved_grav = 10.0f;
        }

        Velocity = Velocity with {Y = saved_grav};
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var movement = Input.GetVector("left", "right", "forward", "backward");
        UpdateDirection(movement, (float)delta);

        Rotation = Rotation with {Y = direction};
    }


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        var movement = Input.GetVector("left", "right", "forward", "backward");

        UpdateState(movement);
        UpdateMovement(movement, (float)delta);

        MoveAndSlide();
    }
}
