using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class Character : CharacterBody3D
{
    private enum State {
        Idle,
        Walking,
        InAir,
        Crouch,
        Shield,
        InAirShield,
        Swimming,
        Flying,
        Float,
        Attack
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

    private const float TURN_ALPHA = 0.01f;

    [Export]
    CameraHing hing = null;

    [Export]
    Stamina stamina = null;

    [Export]
    private Node3D target_node = null;

    private Label3D status_label;

    private CollisionShape3D standing_collision;
    private CollisionShape3D crouching_collision;

    private Node3D standing_model;
    private Node3D crouching_model;

    [Export]
    private ComboDetector combo_detector;

    private const float VELOCITY_LERP_ALPHA = 0.001f;

    private const float TIRED_SPEED = 3.0f;
    private const float WALK_SPEED = 4.5f;
    private const float SPRINT_SPEED = 6.0f;
    private const float FLOAT_SPEED = 2.5f;

    private const float CROUCH_SPEED = 2.0f;

    private const float SHIELD_SPEED = 3.25f;
    private const float FLYING_SPEED = 5.0f;
    private const float FLYING_SPRINT_SPEED = 8.0f;
    private const float FLYING_HEIGHT_SPEED = 3.0f;
    private const float FLYING_HEIGHT_SPRINT_SPEED = 4.0f;

    
    private const float SPRINT_STAMINA = 5.0f;
    private const float FLOAT_STAMINA = 5.0f;
    private const float FLYING_MAX_STAMINA = 10.0f;
    private const float FLYING_MIN_STAMINA = 5.0f;
    private const float FLYING_SPRINT_ADDITIONAL_STAMINA = 1.0f;

    private const float MAX_FLOAT_FALL_SPEED = -1.0f;

    private enum Combo {
        None,
        SimpleLight,
        SimpleHeavy,
        SimpleRange
    };

    private Dictionary<Combo, float> combo_stamina_cost = new Dictionary<Combo, float>(){
        {Combo.None, 0.0f},
        {Combo.SimpleLight, 1.0f},
        {Combo.SimpleHeavy, 5.0f},
        {Combo.SimpleRange, 10.0f}
    };

    private List<Tuple<string[], Combo>> combo_actions = new List<Tuple<string[], Combo>>{
        new Tuple<string[], Combo>(["combo_light"], Combo.SimpleLight),
        new Tuple<string[], Combo>(["combo_heavy"], Combo.SimpleHeavy),
        new Tuple<string[], Combo>(["combo_range"], Combo.SimpleRange)
    };

    private Combo last_combo = Combo.None;

    private AnimationTree animation_tree;

    private Vector3 root_motion;

    public override void _Ready()
    {
        base._Ready();

        status_label = GetNode<Label3D>("State");

        standing_collision = GetNode<CollisionShape3D>("StandingCollision");
        crouching_collision = GetNode<CollisionShape3D>("CrouchingCollision");

        standing_model = GetNode<Node3D>("StandingModel");
        crouching_model = GetNode<Node3D>("CrouchingModel");

        combo_detector.ComboFinalize += () => { ProcessCombos(); };

        animation_tree = GetNode<AnimationTree>("AnimationTree");
    }

    private void ProcessCombos() {
        Combo best_combo = Combo.None;
        int best_idx = int.MaxValue;

        for (int i = 0; i < combo_actions.Count; i++) {
            var (actions, combo) = combo_actions[i];

            var idx = combo_detector.GetCombo(actions);

            if (idx != -1 && idx < best_idx) {
                best_idx = idx;
                best_combo = combo;
            }
        }

        if (best_idx == 0) {
            last_combo = best_combo;
        }
    }

    private void UpdateState(Vector2 movement) {
        var is_moving = movement != Vector2.Zero;
        var crouch_pressed = Input.IsActionPressed("crouch");
        var jump_just_pressed = Input.IsActionJustPressed("jump");
        var shield_pressed = Input.IsActionPressed("shield");
        var on_floor = IsOnFloor();

        var attacked = last_combo != Combo.None;

        var started_flying = false;

        {
            var idx = combo_detector.GetButtonCombo(["combo_jump", "combo_jump"]);

            if (idx == 0) {
                started_flying = true;
            }

            if (started_flying) {
                combo_detector.RemoveButtonComboHistory(2);
            }
        }

        switch (state) {
            case State.Idle:
                if (!on_floor) {
                    state = State.InAir;
                }
                else if (jump_just_pressed) {
                    state = State.InAir;
                }
                else if (attacked) {
                    state = State.Attack;
                }
                else if (crouch_pressed) {
                    state = State.Crouch;
                }
                else if (shield_pressed) {
                    state = State.Shield;
                }
                else if (is_moving) {
                    state = State.Walking;
                }
                break;

            case State.Walking:
                if (!on_floor) {
                    state = State.InAir;
                }
                else if (jump_just_pressed) {
                    state = State.InAir;
                }
                else if (attacked) {
                    state = State.Attack;
                }
                else if (crouch_pressed) {
                    state = State.Crouch;
                }
                else if (shield_pressed) {
                    state = State.Shield;
                }
                else if (!is_moving) {
                    state = State.Idle;
                }
                break;

            case State.InAir:
                if (on_floor) {
                    state = State.Idle;
                }
                else if (shield_pressed) {
                    state = State.InAirShield;
                }
                else if (crouch_pressed && !stamina.IsTired) {
                    state = State.Float;
                }
                else if (started_flying && !stamina.IsTired) {
                    state = State.Flying;
                }
                break;

            case State.Flying:
                if (on_floor) {
                    state = State.Idle;
                }
                else if (stamina.IsTired) {
                    state = State.InAir;
                }
                else if (started_flying) {
                    state = State.InAir;
                }
                break;

            case State.Float:
                if (on_floor) {
                    state = State.Crouch;
                }
                else if (stamina.IsTired) {
                    state = State.InAir;
                }
                else if (!crouch_pressed) {
                    state = State.InAir;
                }
                break;

            case State.Crouch:
                if (!on_floor) {
                    state = State.InAir;
                }
                else if (jump_just_pressed) {
                    state = State.InAir;
                }
                else if (attacked) {
                    state = State.Attack;
                }
                else if (shield_pressed) {
                    state = State.Shield;
                }
                else if (!crouch_pressed) {
                    state = State.Idle;
                }
                break;

            case State.Shield:
                if (!on_floor) {
                    state = State.InAirShield;
                }
                else if (jump_just_pressed) {
                    state = State.InAirShield;
                }
                else if (attacked) {
                    state = State.Attack;
                }
                else if (!shield_pressed) {
                    state = State.Idle;
                }
                break;

            case State.InAirShield:
                if (on_floor) {
                    state = State.Shield;
                }
                else if (!shield_pressed) {
                    state = State.InAir;
                }
                break;

            default:
                throw new NotImplementedException();
        }

        if (state != State.Attack) {
            last_combo = Combo.None;
        }
    }

    private void UpdateDirection(Vector2 movement, float delta) {
        var is_shielding = state == State.Shield || state == State.InAirShield;

        if (target_node != null && is_shielding) {
            var current_pos = new Vector2(GlobalPosition.X, GlobalPosition.Z);
            var target_pos = new Vector2(target_node.GlobalPosition.X, target_node.GlobalPosition.Z);

            target_direction = current_pos.AngleToPoint(target_pos);
            target_direction = -target_direction - (float)Math.PI / 2.0f;
        }
        else if (movement != Vector2.Zero && !is_shielding) {
            var move_direction = movement.Angle();
            move_direction = -move_direction - (float)Math.PI / 2.0f;

            target_direction = move_direction;

            target_direction += hing.direction;
        }

        direction = Mathf.LerpAngle(direction, target_direction, 1.0f - (float)Math.Pow(TURN_ALPHA, delta));
    }

    private void UpdateMovement(Vector2 movement, float delta) {
        Velocity += Vector3.Down * GameSettings.gravity * delta;

        var target_velocity = Velocity;

        var saved_grav = Velocity.Y;

        var forward = -Basis.Z;
        var right = Basis.X;

        var is_moving = movement != Vector2.Zero;

        var just_jumped = Input.IsActionJustPressed("jump");
        var is_jumped = Input.IsActionPressed("jump");

        var just_crouched = Input.IsActionJustPressed("crouch");
        var is_crouched = Input.IsActionPressed("crouch");

        var is_sprinting = Input.IsActionPressed("sprint");

        stamina.drainage = 0.0f;

        switch (state) {
            case State.Idle:
                Velocity = Vector3.Zero;
                break;

            case State.Walking:
                if (is_moving) {
                    if (stamina.IsTired) {
                        target_velocity = root_motion;
                        //target_velocity = forward * TIRED_SPEED * movement.Length();
                    }
                    else if (is_sprinting) {
                        target_velocity = forward * SPRINT_SPEED;
                        stamina.drainage = SPRINT_STAMINA;
                    }
                    else {
                        target_velocity = root_motion;
                        //target_velocity = forward * WALK_SPEED * movement.Length();
                    }
                }
                else {
                    Velocity = Vector3.Zero;
                }
                break;

            case State.InAir:
                break;

            case State.Flying: {
                var amount = Mathf.Remap(movement.Length(), 0.0f, 1.0f, FLYING_MIN_STAMINA, FLYING_MAX_STAMINA);
                stamina.drainage = amount;

                if (is_sprinting) {
                    stamina.drainage += FLYING_SPRINT_ADDITIONAL_STAMINA;
                }

                var speed = FLYING_SPEED;

                if (is_sprinting) {
                    speed = FLYING_SPRINT_SPEED;
                }

                target_velocity = forward * speed * movement.Length();

                var height_speed = FLYING_HEIGHT_SPEED;

                if (is_sprinting) {
                    height_speed = FLYING_HEIGHT_SPRINT_SPEED;
                }

                if (is_jumped) {
                    saved_grav = height_speed;
                }
                else if (is_crouched) {
                    saved_grav = -height_speed;
                }
                else {
                    saved_grav = 0.0f;
                }

                break;
            }


            case State.Float:
                stamina.drainage = FLOAT_STAMINA;
                target_velocity = forward * FLOAT_SPEED * movement.Length();
                saved_grav = Math.Max(saved_grav, MAX_FLOAT_FALL_SPEED);
                break;

            case State.Crouch:
                target_velocity = forward * CROUCH_SPEED * movement.Length();
                break;

            case State.Shield: {
                var move_norm = movement.Normalized();
                var move = forward * move_norm.Dot(Vector2.Up);
                move += right * move_norm.Dot(Vector2.Right);
                target_velocity = move * movement.Length() * SHIELD_SPEED;
                break;
            }

            case State.InAirShield:
                break;

            default:
                throw new NotImplementedException();
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor()) {
            saved_grav = 10.0f;
        }

        target_velocity = root_motion;
        root_motion = Vector3.Zero;

        Velocity = Velocity.Lerp(target_velocity, 1.0f - (float)Math.Pow(VELOCITY_LERP_ALPHA, delta));

        Velocity = Velocity with {Y = saved_grav};
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var movement = Input.GetVector("left", "right", "forward", "backward");
        UpdateDirection(movement, (float)delta);

        Rotation = Rotation with {Y = direction};

        status_label.Text = state.ToString();

        if (state == State.Crouch) {
            standing_collision.Disabled = true;
            standing_model.Visible = false;

            crouching_collision.Disabled = false;
            crouching_model.Visible = true;
        }
        else {
            standing_collision.Disabled = false;
            standing_model.Visible = true;

            crouching_collision.Disabled = true;
            crouching_model.Visible = false;
        }

        root_motion += animation_tree.GetRootMotionPosition();
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
