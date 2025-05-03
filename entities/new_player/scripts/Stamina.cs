using Godot;
using System;

public partial class Stamina : Control
{
    public const float max = 100.0f;

    private float _current = 100.0f;

    public float current {
        get { return _current; }
        set {
            _current = Math.Clamp(value, 0.0f, max);
            value_bar.Value = (_current / max) * 100.0f;

            if (_current == 0.0f) {
                forced_recovery = true;
            }
            else if (_current == 100.0f) {
                forced_recovery = false;
            }
        }
    }

    private float _drainage = 0.0f;

    public float drainage {
        get { return _drainage; }
        set {
            _drainage = value;

            if (value != 0.0f) {
                is_recovering = false;
                start_recovery.Stop();
            }
            else if (start_recovery.IsStopped() && character.IsOnFloor()) {
                start_recovery.Start();
            }
        }
    }

    private bool forced_recovery = false;

    private const float recovery_rate = 10.0f;

    private bool is_recovering = false;

    private ProgressBar value_bar;

    private Timer start_recovery;

    [Export]
    private Character character;

    public bool IsTired {
        get { return forced_recovery || current == 0.0f; }
    }

    public override void _Ready()
    {
        base._Ready();

        start_recovery = GetNode<Timer>("StartRecovery");

        start_recovery.Timeout += () => {
            is_recovering = true;
        };

        value_bar = GetNode<ProgressBar>("Value");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (IsTired) {
            if (start_recovery.IsStopped() && character.IsOnFloor()) {
                start_recovery.Start();
            }
        }
        else {
            current -= drainage * (float)delta;
        }

        if (is_recovering && character.IsOnFloor()) {
            current += recovery_rate * (float)delta;
        }
    }
}
