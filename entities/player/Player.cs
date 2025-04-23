using Godot;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

[GlobalClass]
public partial class Player : CharacterBody3D
{
    public enum State {
        Idle,
        Walking,
        Sprinting,
        Tired,
        Crouching,
        Falling,
        Climbing,
        Sliding,
        ClimbUp,
        ClimbJump,
        Swimming,
        SwimDash,
        Gliding,
        Ragdoll,
        Shielding,
        Death,
        Drowning,
        Jumped,
        JumpShield
    };

    public State state = State.Idle;

    private float GRAVITY = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    [Export]
    public float GAMEPAD_LOOK_SPEED = (float)ProjectSettings.GetSetting("settings/gamepad/look_speed");

    [Export]
    public float MOUSE_LOOK_SPEED = (float)ProjectSettings.GetSetting("settings/mouse/look_speed");

    private Vector2 look_dir;
    private const float LOOK_DIR_MOVEMENT_CHANGE_SPEED = 1.75f;
    private const float LOOK_DIR_MOVEMENT_AIR_CHANGE_SPEED = 0.45f;
    private const float LOOK_DIR_SHIELD_ALPHA = 0.00025f;

    private Node3D camera_base = null;
    private RayCast3D camera_collision_checker = null;
    private Camera3D camera = null;

    private const float CAMERA_COLLISION_MARGIN = 0.5f;
    private const float CAMERA_MAX_ARM_LENGTH = 6.0f;

    private CollisionShape3D character_base = null;
    private float character_dir;
    private const float CHARACTER_TURN_ALPHA = 0.0002f;

    private Vector2 movement;

    private float PLAYER_WALK_SPEED = 4.5f;
    private float PLAYER_SPRINT_SPEED = 8.0f;
    private float PLAYER_TIRED_SPEED = 3.0f;
    private float PLAYER_SWIM_SPEED = 1.4f;
    private float PLAYER_SWIM_DASH_SPEED = 12.0f;
    private float PLAYER_CLIMB_SPEED = 1.25f;
    private float PLAYER_SHIELD_SPEED = 2.75f;
    private float PLAYER_MOVE_ALPHA = 0.001f;
    private float PLAYER_AIR_ALPHA = 0.1f;
    private float PLAYER_WATER_ALPHA = 0.05f;
    private float PLAYER_GLIDE_ALPHA = 0.08f;
    private float PLAYER_CLIMB_JUMP_ALPHA = 0.01f;
    private float PLAYER_SWIM_DASH_ALPHA = 0.01f;
    private float PLAYER_SPRINT_STAMINA = 12.0f;
    private float PLAYER_SWIM_STAMINA = 2.75f;
    private float PLAYER_SWIM_DASH_STAMINA = 4.25f;
    private float PLAYER_GLIDE_MIN_STAMINA = 4.0f;
    private float PLAYER_GLIDE_MAX_STAMINA = 8.0f;
    private float PLAYER_CLIMB_STAMINA = 4.5f;
    private float PLAYER_CLIMB_JUMP_STAMINA = 15.0f;

    private float PLAYER_JUMP_STRENGTH = 6.0f;
    private float PLAYER_WALL_JUMP_STRENGTH = 3.5f;
    private float PLAYER_CLIMB_JUMP_STRENGTH = 12.0f;
    private float TEMP_PLAYER_CLIMBUP_JUMP_STRENGTH = 10.0f;

    private float _current_stamina = 10000.0f;

    private float max_stamina = 100.0f;

    private float stamina_removal_rate = 0.0f;

    private bool stamina_can_recover = false;

    private bool _stamina_was_exhausted = false;
    private bool _stamina_was_recovering = false;
    private bool _stamina_is_recovering = false;

    public float current_stamina {
        get {
            return _current_stamina;
        }
        set {
            stamina_item.Visible = true;
            
            _current_stamina = value;

            if (_current_stamina <= 0.0f) {
                _stamina_was_exhausted = true;
                _current_stamina = 0.0f;
            }
            else if (_current_stamina >= max_stamina) {
                _stamina_was_exhausted = false;
                _current_stamina = max_stamina;
                stamina_hide_timer.Start();
            }
        }
    }

    private const float STAMINA_RECOVERY_AMOUNT = 15.0f;
    private const float STAMINA_MIN_GREEN_PERCENTAGE = 0.2f;

    private ColorRect stamina_item = null;
    private ShaderMaterial stamina_shader = null;
    private Timer stamina_timer = null;
    private Timer stamina_hide_timer = null;

    private Area3D in_water_area = null;
    private Area3D should_float_area = null;
    private Area3D in_death_liquid_area = null;
    private float WATER_BUOYANCY_AMOUNT = 1.0f;
    private float GLIDE_MAX_FALL_SPEED = 1.45f;

    private RayCast3D wall_detector = null;
    private RayCast3D climb_up_detector = null;
    private Marker3D wall_snap_point = null;
    private Marker3D fixed_snap_point = null;

    private float SLIDE_FALL_SPEED = 10.0f;

    [Export]
    private Node3D z_target = null;

    private int bodies_of_water = 0;
    private int bodies_of_floating = 0;

    private Area3D z_lock_check = null;

    private AnimationTree animation_tree = null;

    private Label dbg_state_label = null;

    private void TempKill() {
        GetTree().ReloadCurrentScene();
    }

    public bool IsTired() {
        return _stamina_was_exhausted;
    }

    private void CalculateZTarget() {
        var areas = z_lock_check.GetOverlappingAreas();

        Area3D nearest = null;
        float distance = float.MaxValue;

        foreach (var area in areas) {
            var local_pos = camera.ToLocal(area.GlobalPosition);

            local_pos = local_pos with {Z = 0.0f};

            float area_distance = local_pos.Length();

            if (area_distance < distance || nearest == null) {
                distance = area_distance;
                nearest = area;
            }
        }

        z_target = nearest;
    }

    public override void _Ready()
    {
        base._Ready();

        camera_base = GetNode<Node3D>("CameraBase");
        camera_collision_checker = GetNode<RayCast3D>("CameraBase/CollisionChecker");
        camera = GetNode<Camera3D>("CameraBase/Camera3D");

        character_base = GetNode<CollisionShape3D>("CharacterBase");

        stamina_item = GetNode<ColorRect>("Stamina");
        stamina_shader = (ShaderMaterial)stamina_item.Material;
        stamina_timer = GetNode<Timer>("Stamina/RecoveryTimeout");
        stamina_hide_timer = GetNode<Timer>("Stamina/HideTimer");

        stamina_timer.Timeout += () => _stamina_is_recovering = true;
        stamina_hide_timer.Timeout += () => stamina_item.Visible = false;

        in_water_area = GetNode<Area3D>("InWaterDetector");
        should_float_area = GetNode<Area3D>("WaterFloatDetector");

        in_water_area.AreaEntered += (_) => bodies_of_water += 1;
        in_water_area.AreaExited += (_) => bodies_of_water -= 1;

        should_float_area.AreaEntered += (_) => bodies_of_floating += 1;
        should_float_area.AreaExited += (_) => bodies_of_floating -= 1;

        in_death_liquid_area = GetNode<Area3D>("InDeathLiquidDetector");

        in_death_liquid_area.AreaEntered += (_) => state = State.Drowning;

        wall_detector = GetNode<RayCast3D>("CharacterBase/WallDetector");
        climb_up_detector = GetNode<RayCast3D>("CharacterBase/ClimbUpDetector");

        wall_snap_point = GetNode<Marker3D>("CharacterBase/WallDetector/WallSnapPoint");
        fixed_snap_point = GetNode<Marker3D>("CharacterBase/WallDetector/WallSnapPoint/FixedSnapPoint");

        z_lock_check = GetNode<Area3D>("CharacterBase/ZLockCheck");

        animation_tree = GetNode<AnimationTree>("CharacterBase/AnimationTree");

        dbg_state_label = GetNode<Label>("State");

        current_stamina = max_stamina;
        stamina_item.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsClass("InputEventMouseMotion")) {
            var motion = (InputEventMouseMotion)@event;

            var look_delta = -motion.Relative * 0.001f * MOUSE_LOOK_SPEED;
            look_dir += look_delta;

            look_dir.Y = Math.Clamp(look_dir.Y, (float)-Math.PI / 3.0f, (float)Math.PI / 4.0f);
        }
    }


    public override void _Process(double delta)
    {
        base._Process(delta);

        var look_delta = Input.GetVector("look_right", "look_left", "look_down", "look_up");
        look_delta *= 0.01f * GAMEPAD_LOOK_SPEED;
        look_dir += look_delta;

        look_dir.Y = Math.Clamp(look_dir.Y, (float)-Math.PI / 3.0f, (float)Math.PI / 4.0f);

        movement = Input.GetVector("left", "right", "forward", "backward");

        float angle;

        switch (state) {
            case State.Climbing:
            case State.ClimbJump: {
                var normal = wall_detector.GetCollisionNormal();
                angle = (float)Math.Atan2(normal.X, normal.Z);
                break;
            }

            case State.Shielding:
                if (z_target != null) {
                    var dir_to = ToLocal(z_target.GlobalPosition);
                    angle = (float)Math.Atan2(dir_to.X, dir_to.Z) + (float)Math.PI;
                }
                else {
                    angle = character_dir;
                }
                break;

            case State.JumpShield:
                if (z_target != null) {
                    var dir_to = ToLocal(z_target.GlobalPosition);
                    angle = (float)Math.Atan2(dir_to.X, dir_to.Z) + (float)Math.PI;
                }
                else {
                    angle = character_dir;
                }
                break;

            case State.Ragdoll:
                // TODO
                angle = character_dir;
                break;

            default:
                angle = (float)Math.Atan2(movement.X, movement.Y) + camera_base.Rotation.Y + (float)Math.PI;
                break;
        }

        switch (state) {
            case State.Drowning:
                break;

            case State.Death:
                break;

            case State.Shielding: {
                character_dir = (float)Mathf.LerpAngle(character_dir, angle, 1.0f - Math.Pow(CHARACTER_TURN_ALPHA, (float)delta));

                var target = new Vector2(character_dir, -0.4f);
                var alpha = 1.0f - (float)Math.Pow(LOOK_DIR_SHIELD_ALPHA, delta);
                look_dir.X = Mathf.LerpAngle(look_dir.X, target.X, alpha);
                look_dir.Y = Mathf.LerpAngle(look_dir.Y, target.Y, alpha);
                break;
            }

            case State.JumpShield: {
                character_dir = (float)Mathf.LerpAngle(character_dir, angle, 1.0f - Math.Pow(CHARACTER_TURN_ALPHA, (float)delta));

                var target = new Vector2(character_dir, 0.0f);
                var alpha = 1.0f - (float)Math.Pow(LOOK_DIR_SHIELD_ALPHA, delta);
                look_dir.X = Mathf.LerpAngle(look_dir.X, target.X, alpha);
                look_dir.Y = Mathf.LerpAngle(look_dir.Y, target.Y, alpha);
                break;
            }

            case State.Climbing:
            case State.ClimbJump:
                character_dir = (float)Mathf.LerpAngle(character_dir, angle, 1.0f - Math.Pow(CHARACTER_TURN_ALPHA, (float)delta));
                break;

            default: {
                if (movement != Vector2.Zero) {
                    character_dir = (float)Mathf.LerpAngle(character_dir, angle, 1.0f - Math.Pow(CHARACTER_TURN_ALPHA, (float)delta));
                    if (look_delta == Vector2.Zero) {
                        var look_change_amount = (movement.Dot(Vector2.Down) + 1.0f) / 2.0f;
                        look_change_amount *= movement.Length();
                        
                        Vector2 look_change_dir;

                        if (movement.X < 0.0f) {
                            look_change_dir = Vector2.Right;
                        }
                        else {
                            look_change_dir = Vector2.Left;
                        }

                        float look_dir_speed;

                        if (IsOnFloor()) {
                            look_dir_speed = LOOK_DIR_MOVEMENT_CHANGE_SPEED;
                        }
                        else {
                            look_dir_speed = LOOK_DIR_MOVEMENT_AIR_CHANGE_SPEED;
                        }

                        look_dir.X += look_change_dir.X * look_change_amount * look_dir_speed * (float)delta;
                    }
                }
                break;
            }
        }

        character_base.Rotation = character_base.Rotation with {Y = character_dir};

        camera_base.Rotation = camera.Rotation with {X = look_dir.Y, Y = look_dir.X};

        float camera_arm_length = CAMERA_MAX_ARM_LENGTH;

        if (camera_collision_checker.IsColliding()) {
            var camera_collision_point = camera_collision_checker.GetCollisionPoint();
            camera_collision_point = camera_collision_checker.ToLocal(camera_collision_point);
            camera_arm_length = camera_collision_point.Length() - CAMERA_COLLISION_MARGIN;
        }

        camera.Position = camera_collision_checker.TargetPosition.Normalized() * camera_arm_length;

        if (stamina_removal_rate > 0.0f) {
            current_stamina -= stamina_removal_rate * (float)delta;
            _stamina_is_recovering = false;
        }
        else if (stamina_can_recover) {
            if (!_stamina_was_recovering) {
                stamina_timer.Start();
            }

            if (_stamina_is_recovering) {
                if (current_stamina != max_stamina) {
                    current_stamina += STAMINA_RECOVERY_AMOUNT * (float)delta;
                }
            }
        }

        _stamina_was_recovering = stamina_can_recover;

        var cur_stamina_percentage = current_stamina / max_stamina;
        var green_stamina_percentage = cur_stamina_percentage - stamina_removal_rate / max_stamina;

        if (cur_stamina_percentage < STAMINA_MIN_GREEN_PERCENTAGE || _stamina_was_exhausted) {
            green_stamina_percentage = 0.0f;
        }

        var green_flash = stamina_hide_timer.TimeLeft / stamina_hide_timer.WaitTime;
        green_flash = (1.0f - green_flash) * (float)Math.PI;
        green_flash = (float)Math.Sin(green_flash);

        stamina_shader.SetShaderParameter("red_percentage", cur_stamina_percentage);
        stamina_shader.SetShaderParameter("green_percentage", green_stamina_percentage);

        stamina_shader.SetShaderParameter("green_flash_amount", green_flash);

        animation_tree.Set("parameters/conditions/grounded", IsOnFloor());
        animation_tree.Set("parameters/conditions/falling", state == State.Falling);
        animation_tree.Set("parameters/conditions/jumped", state == State.Jumped);

        switch (state) {
            case State.Idle:
            case State.Walking:
                animation_tree.Set("parameters/Ground/blend_position", movement.Length() / 2.0f);
                break;

            case State.Sprinting:
                if (IsTired()) {
                    animation_tree.Set("parameters/Ground/blend_position", movement.Length() / 2.0f);
                }
                else {
                    animation_tree.Set("parameters/Ground/blend_position", 1.0f);
                }
                break;

            default:
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        var moving_into_wall = false;

        var climb_up = !climb_up_detector.IsColliding();

        var colliding_with_wall = wall_detector.IsColliding();

        if (colliding_with_wall) {
            moving_into_wall = movement.Y < 0.0f;
        }

        var moving = movement != Vector2.Zero;

        var in_water = bodies_of_water != 0;
        var should_float = bodies_of_floating != 0;

        var jumped = Input.IsActionJustPressed("jump");
        var sprinting = Input.IsActionPressed("sprint");
        var sprint_jump = Input.IsActionJustPressed("sprint");
        var shielding = Input.GetActionStrength("shield") >= 0.5f;

        switch (state) {
            case State.Idle:
                if (moving_into_wall && !should_float) {
                    state = State.Climbing;
                }
                else if (in_water) {
                    state = State.Swimming;
                }
                else if (!IsOnFloor()) {
                    state = State.Falling;
                }
                else if (jumped) {
                    if (shielding) {
                        CalculateZTarget();
                        state = State.JumpShield;
                    }
                    else {
                        state = State.Jumped;
                    }
                }
                else if (shielding) {
                    CalculateZTarget();
                    state = State.Shielding;
                }
                else if (moving) {
                    if (IsTired()) {
                        state = State.Tired;
                    }
                    else if (sprinting) {
                        state = State.Sprinting;
                    }
                    else {
                        state = State.Walking;
                    }
                }
                break;

            case State.Walking:
                if (moving_into_wall && !should_float) {
                    state = State.Climbing;
                }
                else if (in_water) {
                    state = State.Swimming;
                }
                else if (!IsOnFloor()) {
                    state = State.Falling;
                }
                else if (jumped) {
                    if (shielding) {
                        CalculateZTarget();
                        state = State.JumpShield;
                    }
                    else {
                        state = State.Jumped;
                    }
                }
                else if (shielding) {
                    CalculateZTarget();
                    state = State.Shielding;
                }
                else if (moving) {
                    if (IsTired()) {
                        state = State.Tired;
                    }
                    else if (sprinting) {
                        state = State.Sprinting;
                    }
                }
                else {
                    state = State.Idle;
                }
                break;

            case State.Sprinting:
                if (moving_into_wall && !should_float) {
                    state = State.Climbing;
                }
                else if (in_water) {
                    state = State.Swimming;
                }
                else if (!IsOnFloor()) {
                    state = State.Falling;
                }
                else if (jumped) {
                    if (shielding) {
                        CalculateZTarget();
                        state = State.JumpShield;
                    }
                    else {
                        state = State.Jumped;
                    }
                }
                else if (shielding) {
                    CalculateZTarget();
                    state = State.Shielding;
                }
                else if (moving) {
                    if (IsTired()) {
                        state = State.Tired;
                    }
                    else if (!sprinting) {
                        state = State.Walking;
                    }
                }
                else {
                    state = State.Idle;
                }
                break;

            case State.Tired:
                if (in_water && !should_float) {
                    state = State.Drowning;
                }
                else if (!IsOnFloor()) {
                    state = State.Falling;
                }
                else if (jumped) {
                    if (shielding) {
                        CalculateZTarget();
                        state = State.JumpShield;
                    }
                    else {
                        state = State.Jumped;
                    }
                }
                else if (shielding) {
                    CalculateZTarget();
                    state = State.Shielding;
                }
                else if (moving) {
                    if (!IsTired()) {
                        if (sprinting) {
                            state = State.Sprinting;
                        }
                        else {
                            state = State.Walking;
                        }
                    }
                }
                else {
                    state = State.Idle;
                }
                break;

            case State.Crouching:
                // TODO
                break;

            case State.Falling:
                if (moving_into_wall && !IsTired()) {
                    if (Velocity.Y <= -SLIDE_FALL_SPEED) {
                        state = State.Sliding;
                    }
                    else {
                        state = State.Climbing;
                    }
                }
                else if (in_water) {
                    state = State.Swimming;
                }
                else if (jumped) {
                    state = State.Gliding;
                }
                else if (IsOnFloor()) {
                    if (moving) {
                        if (IsTired()) {
                            state = State.Tired;
                        }
                        else if (sprinting) {
                            state = State.Sprinting;
                        }
                        else {
                            state = State.Walking;
                        }
                    }
                    else {
                        state = State.Idle;
                    }
                }
                break;

            case State.Climbing:
                if (IsTired()) {
                    state = State.Falling;
                }
                else if (should_float) {
                    state = State.Swimming;
                }
                else if (jumped) {
                    if (movement.Y >= 0.05) {
                        state = State.Jumped;
                        
                        character_dir += (float)Math.PI;
                        
                        var jump_dir = wall_detector.GetCollisionNormal();
                        jump_dir = jump_dir with {Y = 0.0f};
                        jump_dir = jump_dir.Normalized();

                        jump_dir *= PLAYER_WALL_JUMP_STRENGTH;
                        Velocity += jump_dir;
                    }
                    else {
                        state = State.Falling;
                    }
                }
                else if (sprint_jump) {
                    state = State.ClimbJump;

                    if (moving) {
                        var jump_amount = character_base.Basis.X * movement.X;
                        jump_amount -= character_base.Basis.Y * movement.Y;

                        jump_amount *= PLAYER_CLIMB_JUMP_STRENGTH;

                        Velocity = jump_amount;

                        current_stamina -= PLAYER_CLIMB_JUMP_STAMINA;
                    }
                }
                else if (climb_up) {
                    state = State.ClimbUp;
                }
                else if (!colliding_with_wall) {
                    if (IsOnFloor()) {
                        state = State.Idle;
                    }
                    else {
                        state = State.Falling;
                    }
                }
                break;
            
            case State.Sliding:
                // TODO
                state = State.Climbing;
                break;

            case State.ClimbUp:
                state = State.Falling;
                break;

            case State.ClimbJump:
                if (Velocity.Length() <= PLAYER_CLIMB_SPEED) {
                    state = State.Climbing;
                }
                else if (!colliding_with_wall) {
                    if (IsOnFloor()) {
                        state = State.Idle;
                    }
                    else {
                        state = State.Falling;
                    }
                }
                else if (climb_up) {
                    state = State.ClimbJump;
                }
                break;

            case State.Swimming:
                if (IsTired()) {
                    state = State.Drowning;
                }
                else if (!in_water) {
                    if (!moving) {
                        state = State.Idle;
                    }
                    else {
                        if (sprinting) {
                            state = State.Sprinting;
                        }
                        else {
                            state = State.Walking;
                        }
                    }
                }
                else if (!should_float) {
                    if (moving_into_wall) {
                        state = State.Climbing;
                    }
                    else if (sprint_jump) {
                        state = State.SwimDash;

                        var dash_amount = -character_base.Basis.Z;
                        dash_amount *= PLAYER_SWIM_DASH_SPEED;

                        Velocity = dash_amount;

                        current_stamina -= PLAYER_SWIM_DASH_STAMINA;
                    }
                }
                break;
            
            case State.SwimDash:
                if (colliding_with_wall) {
                    state = State.Climbing;
                }
                else if (!in_water) {
                    if (!IsOnFloor()) {
                        state = State.Falling;
                    }
                    if (moving) {
                        if (sprinting) {
                            state = State.Sprinting;
                        }
                        else {
                            state = State.Walking;
                        }
                    }
                    else {
                        state = State.Idle;
                    }
                }
                else if (Velocity.Length() <= PLAYER_SWIM_SPEED) {
                    state = State.Swimming;
                }
                break;

            case State.Gliding:
                if (IsTired()) {
                    state = State.Falling;
                }
                else if (should_float) {
                    state = State.Swimming;
                }
                else if (moving_into_wall) {
                    state = State.Climbing;
                }
                else if (in_water) {
                    state = State.Swimming;
                }
                else if (jumped) {
                    state = State.Falling;
                }
                else if (IsOnFloor()) {
                    if (moving) {
                        if (sprinting) {
                            state = State.Sprinting;
                        }
                        else {
                            state = State.Walking;
                        }
                    }
                    else {
                        state = State.Idle;
                    }
                }
                break;

            case State.Ragdoll:
                // TODO
                break;

            case State.Shielding:
                if (should_float) {
                    state = State.Swimming;
                }
                else if (!shielding && moving_into_wall) {
                    state = State.Climbing;
                }
                else if (in_water) {
                    state = State.Swimming;
                }
                else if (!shielding) {
                    if (!IsOnFloor()) {
                        state = State.Falling;
                    }
                    else if (jumped) {
                        state = State.Jumped;
                    }
                    else if (moving) {
                        if (IsTired()) {
                            state = State.Tired;
                        }
                        else if (sprinting) {
                            state = State.Sprinting;
                        }
                        else {
                            state = State.Walking;
                        }
                    }
                    else {
                        state = State.Idle;
                    }
                }
                else if (jumped && IsOnFloor()) {
                    state = State.JumpShield;
                }
                break;

            case State.Death:
                // TODO
                TempKill();
                break;

            case State.Drowning:
                // TODO
                TempKill();
                break;

            case State.Jumped:
                state = State.Falling;
                break;

            case State.JumpShield:
                state = State.Shielding;
                break;
        }

        dbg_state_label.Text = state.ToString();

        var movement_amount = -character_base.Basis.Z * movement.Length();

        var saved_grav = Velocity.Y;
        var apply_gravity = false;

        switch (state) {
            case State.Idle:
                Velocity = Velocity with {X = 0.0f, Z = 0.0f};
                apply_gravity = true;
                
                stamina_can_recover = true;
                stamina_removal_rate = 0.0f;

                break;

            case State.Walking:
                apply_gravity = true;

                movement_amount *= PLAYER_WALK_SPEED;
                Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_MOVE_ALPHA, delta));
                
                stamina_can_recover = true;
                stamina_removal_rate = 0.0f;

                break;

            case State.Sprinting:
                apply_gravity = true;

                movement_amount = -character_base.Basis.Z;
                movement_amount *= PLAYER_SPRINT_SPEED;
                Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_MOVE_ALPHA, delta));

                stamina_can_recover = false;
                stamina_removal_rate = PLAYER_SPRINT_STAMINA;

                break;

            case State.Tired:
                apply_gravity = true;

                movement_amount *= PLAYER_TIRED_SPEED;
                Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_MOVE_ALPHA, delta));
                
                stamina_can_recover = true;
                stamina_removal_rate = 0.0f;

                break;

            case State.Crouching:
                apply_gravity = true;

                // TODO
                break;

            case State.Falling:
                apply_gravity = true;

                stamina_can_recover = false;
                stamina_removal_rate = 0.0f;

                movement_amount *= PLAYER_WALK_SPEED;

                Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_AIR_ALPHA, delta));

                break;

            case State.Climbing: {
                apply_gravity = false;

                movement_amount = character_base.Basis.X * movement.X;
                movement_amount -= character_base.Basis.Y * movement.Y;

                movement_amount *= PLAYER_CLIMB_SPEED;

                Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_MOVE_ALPHA, delta));

                stamina_can_recover = false;
                stamina_removal_rate = PLAYER_CLIMB_STAMINA * movement.Length();

                wall_snap_point.GlobalPosition = wall_detector.GetCollisionPoint();
                var offset = ToLocal(fixed_snap_point.GlobalPosition);
                offset = offset with {Y = 0.0f};

                Velocity += offset;

                saved_grav = Velocity.Y;
                break;
            }

            case State.Sliding:
                // TODO
                break;

            case State.ClimbUp:
                apply_gravity = false;

                saved_grav = TEMP_PLAYER_CLIMBUP_JUMP_STRENGTH;

                stamina_can_recover = false;
                stamina_removal_rate = 0.0f;

                break;

            case State.ClimbJump: {
                apply_gravity = false;

                Velocity = Velocity.Lerp(Vector3.Zero, 1.0f - (float)Math.Pow(PLAYER_CLIMB_JUMP_ALPHA, delta));

                wall_snap_point.GlobalPosition = wall_detector.GetCollisionPoint();
                var offset = ToLocal(fixed_snap_point.GlobalPosition);
                offset = offset with {Y = 0.0f};

                Velocity += offset;

                saved_grav = Velocity.Y;

                break;
            }

            case State.Swimming:
                apply_gravity = false;

                if (should_float) {
                    saved_grav = WATER_BUOYANCY_AMOUNT;
                }
                else {
                    saved_grav = 0.0f;
                }

                if (moving) {
                    movement_amount *= PLAYER_SWIM_SPEED;
                    Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_WATER_ALPHA, delta));

                    stamina_removal_rate = PLAYER_SWIM_STAMINA;
                }
                else {
                    Velocity = Vector3.Zero;

                    stamina_removal_rate = 0.0f;
                }

                stamina_can_recover = false;

                break;

            case State.SwimDash:
                apply_gravity = false;

                saved_grav = 0.0f;

                Velocity = Velocity.Lerp(Vector3.Zero, 1.0f - (float)Math.Pow(PLAYER_SWIM_DASH_ALPHA, delta));

                break;

            case State.Gliding:
                apply_gravity = false;

                saved_grav += -GRAVITY * (float)delta;

                saved_grav = Math.Max(saved_grav, -GLIDE_MAX_FALL_SPEED);

                movement_amount *= PLAYER_WALK_SPEED;
                Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_GLIDE_ALPHA, delta));

                stamina_removal_rate = Mathf.Lerp(PLAYER_GLIDE_MIN_STAMINA, PLAYER_GLIDE_MAX_STAMINA, movement.Length());
                stamina_can_recover = false;
                
                break;

            case State.Ragdoll:
                apply_gravity = true;

                // TODO
                break;

            case State.Shielding:
                apply_gravity = true;

                movement_amount = character_base.Basis.X * movement.X;
                movement_amount += character_base.Basis.Z * movement.Y;

                movement_amount *= PLAYER_SHIELD_SPEED;
                Velocity = Velocity.Lerp(movement_amount, 1.0f - (float)Math.Pow(PLAYER_MOVE_ALPHA, delta));

                stamina_removal_rate = 0.0f;
                stamina_can_recover = true;
                
                break;

            case State.Death:
                apply_gravity = true;

                // TODO
                break;

            case State.Drowning:
                // TODO
                break;

            case State.Jumped:
                saved_grav = PLAYER_JUMP_STRENGTH;

                stamina_can_recover = false;
                stamina_removal_rate = 0.0f;
                
                break;

            case State.JumpShield:
                saved_grav = PLAYER_JUMP_STRENGTH;

                stamina_can_recover = false;
                stamina_removal_rate = 0.0f;

                break;
        }

        Velocity = Velocity with {Y = saved_grav};

        if (apply_gravity) {
            Velocity += Vector3.Down * GRAVITY * (float)delta;
        }

        MoveAndSlide();
    }
}
