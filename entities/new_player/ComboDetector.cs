using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ComboDetector : Node
{
    private List<Tuple<int, bool>> action_history = new List<Tuple<int, bool>>();
    private List<string> button_action_history = new List<string>();
    private List<string> axis_action_history = new List<string>();

    private Timer clear_history_timer;

    private Godot.Collections.Array<StringName> actions;

    [Export]
    private int MAX_ACTIONS = 5;

    [Export]
    private float BUTTON_CLEAR_TIMER = 0.2f;

    [Export]
    private float AXIS_CLEAR_TIMER = 0.35f;

    [Signal]
    public delegate void ComboFinalizeEventHandler();

    [Export]
    public Label3D label;

    private void UpdateLabel() {
        var str = "[";

        for (int i = 0; i < action_history.Count; i++) {
            var (idx, is_joy) = action_history[i];
            
            if (is_joy) {
                if (idx >= axis_action_history.Count) {
                    GD.PushWarning($"Invalid axis index {idx}");
                }
                else {
                    str += axis_action_history[idx];
                }
            }
            else {
                if (idx >= button_action_history.Count) {
                    GD.PushWarning($"Invalid button index {idx}");
                }
                else {
                    str += button_action_history[idx];
                }
            }

            if ((i + 1) != action_history.Count) {
                str += ", ";
            }
        }

        str += "]";

        label.Text = str;
    }

    public override void _Ready()
    {
        base._Ready();

        clear_history_timer = GetNode<Timer>("Timer");

        clear_history_timer.Timeout += () => {
            EmitSignal(SignalName.ComboFinalize);
            axis_action_history.Clear();
            button_action_history.Clear();
            action_history.Clear();
            UpdateLabel();
        };

        actions = InputMap.GetActions();

        for (int i = 0; i < actions.Count;) {
            var name = actions[i].ToString();

            if (!name.StartsWith("combo_")) {
                actions.RemoveAt(i);
            }
            else {
                i++;
            }
        }

        UpdateLabel();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        var is_joy = false;

        if (@event.IsClass("InputEventJoypadMotion")) {
            var joy = (InputEventJoypadMotion)@event;

            if (Math.Abs(joy.AxisValue) < 0.85f) {
                return;
            }

            is_joy = true;
        }
        else if (!@event.IsPressed() || @event.IsEcho()) {
            return;
        }


        foreach (var action in actions) {
            if (InputMap.ActionHasEvent(action, @event)) {
                var name = action.ToString();

                if (is_joy && axis_action_history.Count != 0) {
                    if (axis_action_history[axis_action_history.Count-1] == name) {
                        break;
                    }
                }

                if (is_joy) {
                    action_history.Add(new Tuple<int, bool>(axis_action_history.Count, true));
                    axis_action_history.Add(name);
                }
                else {
                    action_history.Add(new Tuple<int, bool>(button_action_history.Count, false));
                    button_action_history.Add(name);
                }

                if (action_history.Count > MAX_ACTIONS) {
                    var (idx, is_axis) = action_history[0];
                    action_history.RemoveAt(0);

                    if (is_axis) {
                        axis_action_history.RemoveAt(idx);

                        for (int i = 0; i < action_history.Count; i++) {
                            if (action_history[i].Item2) {
                                if (action_history[i].Item1 >= idx) {
                                    action_history[i] = new Tuple<int, bool>(
                                        action_history[i].Item1 - 1,
                                        true
                                    );
                                }
                            }
                        }
                    }
                    else {
                        button_action_history.RemoveAt(idx);

                        for (int i = 0; i < action_history.Count; i++) {
                            if (!action_history[i].Item2) {
                                if (action_history[i].Item1 >= idx) {
                                    action_history[i] = new Tuple<int, bool>(
                                        action_history[i].Item1 - 1,
                                        false
                                    );
                                }
                            }
                        }
                    }
                }

                if (is_joy) {
                    clear_history_timer.Start(AXIS_CLEAR_TIMER);
                }
                else {
                    clear_history_timer.Start(BUTTON_CLEAR_TIMER);
                }

                UpdateLabel();

                break;
            }
        }
    }

    public int GetButtonCombo(string[] combo_list) {
        var count = button_action_history.Count - combo_list.Length + 1;

        for (int i = 0; i < count; i++) {
            var found = true;

            for (int j = 0; j < combo_list.Length; j++) {
                if (button_action_history[i + j] != combo_list[j]) {
                    found = false;
                    break;
                }
            }

            if (found) {
                return i;
            }
        }

        return -1;
    }

    public void RemoveButtonComboHistory(int count = -1) {
        if (count == -1) {
            button_action_history.Clear();
            for (int i = 0; i < action_history.Count;) {
                if (!action_history[i].Item2) {
                    action_history.RemoveAt(i);
                }
                else {
                    i++;
                }
            }
        }
        else {
            count = count < button_action_history.Count ? count : button_action_history.Count;
            button_action_history.RemoveRange(0, count);
            for (int i = 0, j = 0; i < action_history.Count && j < count;) {
                if (!action_history[i].Item2) {
                    action_history.RemoveAt(i);
                    j++;
                }
                else {
                    i++;
                }
            }
        }

        for (int i = 0; i < action_history.Count; i++) {
            if (!action_history[i].Item2) {
                action_history[i] = new Tuple<int, bool>(action_history[i].Item1 - 1, false);
            }
        }

        UpdateLabel();
    }

    public int GetAxisCombo(string[] combo_list) {
        var count = axis_action_history.Count - combo_list.Length + 1;

        for (int i = 0; i < count; i++) {
            var found = true;

            for (int j = 0; j < combo_list.Length; j++) {
                if (axis_action_history[i + j] != combo_list[j]) {
                    found = false;
                    break;
                }
            }

            if (found) {
                return i;
            }
        }

        return -1;
    }

    public void RemoveAxisComboHistory(int count = -1) {
        if (count == -1) {
            axis_action_history.Clear();
            for (int i = 0; i < action_history.Count;) {
                if (action_history[i].Item2) {
                    action_history.RemoveAt(i);
                }
                else {
                    i++;
                }
            }
        }
        else {
            count = count < axis_action_history.Count ? count : axis_action_history.Count;
            axis_action_history.RemoveRange(0, count);
            for (int i = 0, j = 0; i < action_history.Count && j < count;) {
                if (action_history[i].Item2) {
                    action_history.RemoveAt(i);
                    j++;
                }
                else {
                    i++;
                }
            }
        }

        for (int i = 0; i < action_history.Count; i++) {
            if (action_history[i].Item2) {
                action_history[i] = new Tuple<int, bool>(action_history[i].Item1 - 1, true);
            }
        }

        UpdateLabel();
    }

    public int GetCombo(string[] combo_list) {
        var count = action_history.Count - combo_list.Length + 1;
        for (int i = 0; i < count; i++) {
            var found = true;

            for (int j = 0; j < combo_list.Length; j++) {
                var (idx, is_axis) = action_history[i + j];
                if (is_axis) {
                    if (axis_action_history[idx] != combo_list[j]) {
                        found = false;
                        break;
                    }
                }
                else {
                    if (button_action_history[idx] != combo_list[j]) {
                        found = false;
                        break;
                    }
                }
            }

            if (found) {
                return i;
            }
        }

        return -1;
    }

    public void RemoveComboHistory(int count = -1) {
        clear_history_timer.Stop();

        if (count == -1) {
            action_history.Clear();
            axis_action_history.Clear();
            button_action_history.Clear();
        }
        else {
            for (int i = 0; i < action_history.Count; i++) {
                var (idx, is_axis) = action_history[i];

                if (is_axis) {
                    axis_action_history.RemoveAt(idx);
                }
                else {
                    button_action_history.RemoveAt(idx);
                }
            }

            action_history.RemoveRange(0, count);
        }
    }

    public void RemoveHistory(int count = -1) {
        clear_history_timer.Stop();

        if (count == -1) {
            action_history.Clear();
        }
        else {
            action_history.RemoveRange(0, count);
        }

        UpdateLabel();
    }

}
