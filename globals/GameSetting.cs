using System;
using Godot;

[GlobalClass]
public partial class GameSettings : Node
{
    private const string GRAVITY_NAME = "physics/3d/default_gravity";
    private const string GAMEPAD_LOOK_SPEED = "settings/gamepad/look_speed";
    private const string MOUSE_LOOK_SPEED = "settings/mouse/look_speed";

    static private float _gravity = float.NaN;
    
    static public float gravity {
        get {
            if (float.IsNaN(_gravity)) {
                _gravity = (float)ProjectSettings.GetSetting(GRAVITY_NAME);
            }

            return _gravity;
        }

        set {
            ProjectSettings.SetSetting(GRAVITY_NAME, value);
            _gravity = value;
        }
    }

    static private float _gamepad_look_speed = float.NaN;
    
    static public float gamepad_look_speed {
        get {
            if (float.IsNaN(_gamepad_look_speed)) {
                _gamepad_look_speed = (float)ProjectSettings.GetSetting(GAMEPAD_LOOK_SPEED);
            }
            return _gamepad_look_speed;
        }
        set {
            ProjectSettings.SetSetting(GAMEPAD_LOOK_SPEED, value);
            _gamepad_look_speed = value;
        }
    }

    static private float _mouse_look_speed = float.NaN;

    static public float mouse_look_speed {
        get {
            if (float.IsNaN(_mouse_look_speed)) {
                _mouse_look_speed = (float)ProjectSettings.GetSetting(MOUSE_LOOK_SPEED);
            }
            return _mouse_look_speed;
        }
        set {
            ProjectSettings.SetSetting(MOUSE_LOOK_SPEED, value);
            _mouse_look_speed = value;
        }
    }
}