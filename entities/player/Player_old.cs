using Godot;
using System;

public partial class PlayerOld : CharacterBody3D
{
    private float GRAVITY = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    public float GAMEPAD_LOOK_SPEED = (float)ProjectSettings.GetSetting("settings/gamepad/look_speed");

    private Vector2 look_dir;
    private const float LOOK_DIR_MOVEMENT_CHANGE_SPEED = 1.75f;
    private const float LOOK_DIR_MOVEMENT_AIR_CHANGE_SPEED = 0.45f;

    private const float HALF_PI = (float)Math.PI / 2.0f;

    private Node3D camera_base = null;
    private RayCast3D camera_collision_checker = null;
    private Camera3D camera = null;

    private const float CAMERA_COLLISION_MARGIN = 0.5f;
    private const float CAMERA_MAX_ARM_LENGTH = 6.0f;

    private Node3D character_base = null;
    private float character_dir;
    private const float CHARACTER_TURN_ALPHA = 0.0002f;

    private Vector2 movement;

    private float PLAYER_WALK_SPEED = 4.5f;
    private float PLAYER_SPRINT_SPEED = 8.0f;
    private float PLAYER_TIRED_SPEED = 3.0f;
    private float PLAYER_SWIM_SPEED = 3.25f;
    private float PLAYER_CLIMB_SPEED = 0.65f;
    private float PLAYER_MOVE_ALPHA = 0.001f;
    private float PLAYER_AIR_ALPHA = 0.1f;
    private float PLAYER_WATER_ALPHA = 0.05f;
    private float PLAYER_GLIDE_ALPHA = 0.08f;
    private float PLAYER_SPRINT_STAMINA = 12.0f;
    private float PLAYER_SWIM_STAMINA = 10.0f;
    private float PLAYER_GLIDE_STAMINA = 8.0f;
    private float PLAYER_CLIMB_STAMINA = 4.5f;

    private float PLAYER_JUMP_STRENGTH = 6.0f;

    [Export]
    private float stamina = 100.0f;
    private float max_stamina = 100.0f;

    [Export]
    private float stamina_removal_amount = 0.0f;

    [Export]
    private bool can_stamina_recover = false;

    [Export]
    private bool is_stamina_recovering = false;
    private const float STAMINA_RECOVERY_AMOUNT = 15.0f;
    private const float STAMINA_MIN_GREEN_PERCENTAGE = 0.2f;

    private MeshInstance3D stamina_mesh = null;
    private ShaderMaterial stamina_shader = null;
    private Timer stamina_timer = null;
    private Timer stamina_hide_timer = null;

    private Area3D in_water_area = null;
    private Area3D should_float_area = null;
    private bool in_water = false;
    private bool apply_buoyancy = false;
    private float WATER_BUOYANCY_AMOUNT = 1.0f;

    private bool is_gliding = false;
    private float GLIDE_MAX_FALL_SPEED = 1.45f;

    private RayCast3D wall_detector = null;
    private RayCast3D climb_up_detector = null;
    private Marker3D wall_snap_point = null;
    private Marker3D fixed_snap_point = null;

    private bool is_climbing = false;

    private bool _in_cutscene = false;

    [Export]
    public bool in_cutscene {
        get { return _in_cutscene; }
        set { _in_cutscene = value; }
    }

    private bool _force_show_stamina = false;

    [Export]
    private bool force_show_stamina {
        get { return _force_show_stamina; }
        set {
            _force_show_stamina = value;

            if (value) {
                stamina_mesh.Visible = true;
            }
            else if (stamina == max_stamina) {
                stamina_mesh.Visible = false;
            }
        }
    }

    public void SetStaminaRemoval(float amount) {
        stamina_removal_amount = amount;
        stamina_removal_amount = Math.Max(stamina_removal_amount, 0.0f);
        can_stamina_recover = false;
        stamina_mesh.Visible = true;
        stamina_hide_timer.Stop();
    }

    public void RecoverStamina() {
        if (stamina_removal_amount != 0.0f) {
            if (stamina <= 0.0) {
                is_stamina_recovering = true;
            }

            stamina_removal_amount = 0.0f;
            stamina_timer.Start();
        }
    }

    public float GetStamina() {
        if (is_stamina_recovering) {
            return 0.0f;
        }

        return stamina;
    }

    public bool IsTired() {
        return is_stamina_recovering || stamina <= 0.0f;
    }

    private void TempKill() {
        GetTree().ReloadCurrentScene();
    }

    public override void _Ready()
    {
        base._Ready();

        camera_base = GetNode<Node3D>("CameraBase");
        camera_collision_checker = GetNode<RayCast3D>("CameraBase/CollisionChecker");
        camera = GetNode<Camera3D>("CameraBase/Camera3D");

        character_base = GetNode<Node3D>("CharacterBase");

        stamina_mesh = GetNode<MeshInstance3D>("CameraBase/Camera3D/Stamina");
        stamina_shader = (ShaderMaterial)stamina_mesh.GetSurfaceOverrideMaterial(0);
        stamina_timer = GetNode<Timer>("CameraBase/Camera3D/Stamina/RecoveryTimeout");
        stamina_hide_timer = GetNode<Timer>("CameraBase/Camera3D/Stamina/HideTimer");

        stamina_timer.Timeout += () => can_stamina_recover = true;
        stamina_hide_timer.Timeout += () => stamina_mesh.Visible = false;

        in_water_area = GetNode<Area3D>("InWaterDetector");
        should_float_area = GetNode<Area3D>("WaterFloatDetector");

        in_water_area.AreaEntered += (_) => in_water = true;
        in_water_area.AreaExited += (_) => in_water = false;

        should_float_area.AreaEntered += (_) => apply_buoyancy = true;
        should_float_area.AreaExited += (_) => apply_buoyancy = false;

        wall_detector = GetNode<RayCast3D>("CharacterBase/WallDetector");
        climb_up_detector = GetNode<RayCast3D>("CharacterBase/ClimbUpDetector");

        wall_snap_point = GetNode<Marker3D>("CharacterBase/WallDetector/WallSnapPoint");
        fixed_snap_point = GetNode<Marker3D>("CharacterBase/WallDetector/FixedSnapPoint");
    }


    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!in_cutscene) {
            var look_delta = Input.GetVector("look_right", "look_left", "look_down", "look_up");
            look_delta *= 0.01f * GAMEPAD_LOOK_SPEED;
            look_dir += look_delta;

            look_dir.Y = Math.Clamp(look_dir.Y, -HALF_PI, HALF_PI);

            movement = Input.GetVector("left", "right", "forward", "backward");

            var angle = (float)Math.Atan2(movement.X, movement.Y) + camera_base.Rotation.Y + (float)Math.PI;

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

            if (is_climbing) {
                var normal = wall_detector.GetCollisionNormal();
                normal = normal with {Y = 0.0f};
                normal = normal.Normalized();
                
                var normal_angle = (float)Math.Atan2(normal.X, normal.Z);
                character_base.Rotation = character_base.Rotation with {Y = normal_angle};
            }
            else {
                character_base.Rotation = character_base.Rotation with {Y = character_dir};
            }

            camera_base.Rotation = camera.Rotation with {X = look_dir.Y, Y = look_dir.X};

            float camera_arm_length = CAMERA_MAX_ARM_LENGTH;

            if (camera_collision_checker.IsColliding()) {
                var camera_collision_point = camera_collision_checker.GetCollisionPoint();
                camera_collision_point = camera_collision_checker.ToLocal(camera_collision_point);
                camera_arm_length = camera_collision_point.Length() - CAMERA_COLLISION_MARGIN;
            }

            camera.Position = camera_collision_checker.TargetPosition.Normalized() * camera_arm_length;
        }

        var cur_stamina_percentage = stamina / max_stamina;
        var green_stamina_percentage = cur_stamina_percentage - stamina_removal_amount / max_stamina;

        if (cur_stamina_percentage < STAMINA_MIN_GREEN_PERCENTAGE || is_stamina_recovering) {
            green_stamina_percentage = 0.0f;
        }

        stamina_shader.SetShaderParameter("red_percentage", cur_stamina_percentage);
        stamina_shader.SetShaderParameter("green_percentage", green_stamina_percentage);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!in_cutscene) {
        if (Input.IsActionJustPressed("temp_climb") && is_climbing) {
            is_climbing = false;
        }
        else if (wall_detector.IsColliding() && Input.IsActionJustPressed("temp_climb")) {
            is_climbing = true;
        }

        if (IsTired()) {
            is_climbing = false;
        }

        if (in_water) {
            if (apply_buoyancy) {
                Velocity = Velocity with {Y = WATER_BUOYANCY_AMOUNT};
                is_climbing = false;
            }
            else {
                Velocity = Velocity with {Y = Math.Max(Velocity.Y, 0.0f)};
            }
        }
        else if (is_climbing) {
            Velocity = Vector3.Zero;
        }
        else {
            Velocity += Vector3.Down * GRAVITY * (float)delta;

            if (is_gliding) {
                Velocity = Velocity with {Y = Math.Max(Velocity.Y, -GLIDE_MAX_FALL_SPEED)};
            }
        }

        bool was_gliding = is_gliding;

        if (IsOnFloor() || in_water || IsTired() || is_climbing) {
            is_gliding = false;
        }

        RecoverStamina();

        if (is_gliding) {
            SetStaminaRemoval(PLAYER_GLIDE_STAMINA);
        }
        else if (was_gliding) {
            SetStaminaRemoval(0.0f);
        }

        if (is_climbing) {
                Velocity = Vector3.Zero;

                if (movement != Vector2.Zero) {
                    Vector3 target_velocity = movement.Y * -Vector3.Up;
                    target_velocity += movement.X * character_base.Basis.X;
                    
                    target_velocity *= PLAYER_CLIMB_SPEED;
                    Velocity = target_velocity;
                    SetStaminaRemoval(PLAYER_CLIMB_STAMINA);
                }
            }
            else if (IsOnFloor()) {
                if (Input.IsActionJustPressed("jump")) {
                    Velocity += Vector3.Up * PLAYER_JUMP_STRENGTH;
                }
                else {
                    var saved_grav = Velocity.Y;
                    if (movement == Vector2.Zero) {
                        Velocity = Vector3.Zero;
                    }
                    else {
                        var speed = movement.Length();
                        var direction = -character_base.Basis.Z * speed;

                        Velocity = Velocity with {Y = 0.0f};

                        var target_velocity = direction;
                        if (Input.IsActionPressed("sprint") && !IsTired()) {
                            SetStaminaRemoval(PLAYER_SPRINT_STAMINA);
                            target_velocity *= PLAYER_SPRINT_SPEED;
                        }
                        else {
                            if (IsTired()) {
                                target_velocity *= PLAYER_TIRED_SPEED;
                            }
                            else {
                                target_velocity *= PLAYER_WALK_SPEED;
                            }

                            if (!Input.IsActionPressed("sprint")) {
                                RecoverStamina();
                            }
                        }

                        Velocity = Velocity.Lerp(target_velocity, 1.0f - (float)Math.Pow(PLAYER_MOVE_ALPHA, delta));
                    }
                    Velocity = Velocity with {Y = saved_grav};
                }
            }
            else if (in_water) {
                if (movement != Vector2.Zero) {
                    var speed = movement.Length();
                    var direction = -character_base.Basis.Z * speed;

                    var saved_grav = Velocity.Y;

                    Velocity = Velocity with {Y = 0.0f};

                    var target_velocity = direction * PLAYER_SWIM_SPEED;

                    Velocity = Velocity.Lerp(target_velocity, 1.0f - (float)Math.Pow(PLAYER_WATER_ALPHA, delta));

                    Velocity = Velocity with {Y = saved_grav};

                    SetStaminaRemoval(PLAYER_SWIM_STAMINA);
                }
                else {
                    Velocity = Velocity with {X = 0.0f, Z = 0.0f};
                    SetStaminaRemoval(0.0f);
                }

                if (IsTired()) {
                    TempKill();
                }
            }
            else {
                if (Input.IsActionJustPressed("jump") && !IsTired()) {
                    is_gliding = !is_gliding;

                    if (!is_gliding) {
                        SetStaminaRemoval(0.0f);
                    }
                }

                if (movement != Vector2.Zero) {
                    var speed = movement.Length();
                    var direction = -character_base.Basis.Z * speed;

                    var saved_grav = Velocity.Y;

                    Velocity = Velocity with {Y = 0.0f};

                    var target_velocity = direction * PLAYER_WALK_SPEED;

                    float alpha;

                    if (is_gliding) {
                        alpha = PLAYER_GLIDE_ALPHA;
                    }
                    else {
                        alpha = PLAYER_AIR_ALPHA;
                    }

                    Velocity = Velocity.Lerp(target_velocity, 1.0f - (float)Math.Pow(alpha, delta));

                    Velocity = Velocity with {Y = saved_grav};
                }
            }
        }

        if (is_climbing) {
            var wall_point = wall_detector.GetCollisionPoint();
            wall_snap_point.GlobalPosition = wall_point;

            var needed_movement = wall_snap_point.Position - fixed_snap_point.Position;
            Velocity += needed_movement;
        }

        if (!in_cutscene) {
            MoveAndSlide();
        }
    }


}
