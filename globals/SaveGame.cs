global using GameSaveData = Godot.Collections.Dictionary<string, Godot.Variant>;

using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection;

[GlobalClass]
public partial class SaveGame : Node
{

    private const float AUTO_SAVE_TIME = 10 * 60; // 10 minutes

    private static Timer auto_save_timer;

    private const bool AUTO_SAVE_ENABLED = false;

    private const string base_save_path = "user://save/";

    private const string save_file_list = base_save_path + "saves.dat";

    private static List<Tuple<string, bool>> save_files = new List<Tuple<string, bool>>();
    private static SceneTree tree;

    private const int MAX_QUICK_SAVES = 5;
    private const int MAX_MANUAL_SAVES = 5;

    private static byte[] save_key = {
        0xcb, 0x82, 0x19, 0x8b, 0x19, 0x64, 0x3d, 0xcc, 0x64, 0xe6, 0xb3, 0x48, 0xa3, 0x0f, 0x84, 0x0f,
        0xe4, 0x07, 0x4e, 0x15, 0xb0, 0x27, 0xe0, 0x7d, 0x21, 0x66, 0x72, 0xd9, 0x91, 0x9a, 0x26, 0x26,
    };

    [Signal]
    public delegate void AutosaveEventHandler();

    public override void _Ready()
    {
        base._Ready();

        tree = GetTree();

        if (!AUTO_SAVE_ENABLED) {
            GD.PushWarning("Autosave is disabled");
        }

        auto_save_timer = new Timer();
        AddChild(auto_save_timer);
        auto_save_timer.Start(AUTO_SAVE_TIME);

        auto_save_timer.Timeout += () => {
            if (AUTO_SAVE_ENABLED) {
                EmitSignal(SignalName.Autosave);
                Save(true);
            }
        };

        var master_file = FileAccess.OpenEncrypted(save_file_list, FileAccess.ModeFlags.Read, save_key);

        if (master_file != null) {
            var contents = master_file.GetAsText(true);
            var lines = contents.Split(" ");

            foreach (var line in lines) {
                if (line.Length == 0) {
                    continue;
                }

                var parts = line.Split(";");

                if (parts[1] == "0") {
                    save_files.Add(new Tuple<string, bool>(parts[0], false));
                }
                else {
                    save_files.Add(new Tuple<string, bool>(parts[0], true));
                }
            }

            master_file.Close();
        }

        if (!DirAccess.DirExistsAbsolute(base_save_path)) {
            DirAccess.MakeDirAbsolute(base_save_path);
        }
    }

    public static bool Save(bool is_quick_save = false) {
        auto_save_timer.Start(AUTO_SAVE_TIME);

        var file_name = $"{Guid.NewGuid()}.dat";
        var save_path = $"{base_save_path}{file_name}";

        var master_file = FileAccess.OpenEncrypted(save_file_list, FileAccess.ModeFlags.Write, save_key);

        if (master_file == null) {
            GD.PushError($"Cannot open {save_file_list}");
            return false;
        }

        var file = FileAccess.OpenEncrypted(save_path, FileAccess.ModeFlags.Write, save_key);

        if (file == null) {
            GD.PushError($"Cannot save to {save_path}");
            return false;
        }

        var nodes = tree.GetNodesInGroup("Persistent");

        foreach (var node in nodes) {
            if (string.IsNullOrEmpty(node.SceneFilePath)) {
                GD.PushWarning($"Node {node.Name} is not an instanced scene, skipped");
                continue;
            }

            if (!node.HasMethod("Save")) {
                GD.PushWarning($"Node {node.Name} does not have a Save() function, skipped");
                continue;
            }

            var data = (GameSaveData)node.Call("Save");
            data.Add("Filename", node.SceneFilePath);
            data.Add("Parent", node.GetParent().GetPath());
            data.Add("Name", node.Name);

            var json_str = Json.Stringify(data);

            file.StoreLine(json_str);
        }

        file.Close();

        save_files.Insert(0, new Tuple<string, bool>(file_name, is_quick_save));

        if (is_quick_save) {
            var count = 0;
            var last_idx = 0;
            for (int i = 0; i < save_files.Count; i++) {
                if (save_files[i].Item2) {
                    count++;
                    last_idx = i;
                }
            }

            if (count > MAX_QUICK_SAVES) {
                DirAccess.RemoveAbsolute($"{base_save_path}{save_files[last_idx].Item1}");
                save_files.RemoveAt(last_idx);
            }
        }
        else {
            var count = 0;
            var last_idx = 0;
            for (int i = 0; i < save_files.Count; i++) {
                if (!save_files[i].Item2) {
                    count++;
                    last_idx = i;
                }
            }

            if (count > MAX_MANUAL_SAVES) {
                DirAccess.RemoveAbsolute($"{base_save_path}{save_files[last_idx].Item1}");
                save_files.RemoveAt(last_idx);
            }
        }

        foreach (var save in save_files) {
            master_file.StoreString($"{save.Item1};");
            if (save.Item2) {
                master_file.StoreString($"1 ");
            }
            else {
                master_file.StoreString($"0 ");
            }
        }

        master_file.Close();

        return true;
    }

    public static List<bool> GetSaveHistory() {
        var saves = new List<bool>();

        return saves;
    }

    public static bool Load(int history_idx) {
        if (history_idx >= save_files.Count) {
            return false;
        }

        auto_save_timer.Start(AUTO_SAVE_TIME);

        var path = $"{base_save_path}{save_files[history_idx].Item1}";
        
        var file = FileAccess.OpenEncrypted(path, FileAccess.ModeFlags.Read, save_key);

        if (file == null) {
            GD.PushError($"Cannot open save file {history_idx}");
            return false;
        }

        var nodes = tree.GetNodesInGroup("Persistent");

        foreach (var node in nodes) {
            node.Name = $"{node.Name}~FreeQueued";
            node.QueueFree();
        }

        while (file.GetPosition() < file.GetLength()) {
            var json_str = file.GetLine();

            var json = new Json();
            var parse_result = json.Parse(json_str);

            if (parse_result != Error.Ok) {
                GD.PushError($"Json parse error: {json.GetErrorMessage} in {json_str}");
                continue;
            }

            var node_data = new GameSaveData((Godot.Collections.Dictionary)json.Data);

            var obj = GD.Load<PackedScene>(node_data["Filename"].ToString());

            if (obj == null) {
                GD.PushError($"Cannot load {node_data["Filename"]}");
                continue;
            }

            var node = obj.Instantiate<Node>();

            tree.Root.GetNode(node_data["Parent"].ToString()).AddChild(node);

            node.Name = node_data["Name"].ToString();

            if (!node.HasMethod("Load")) {
                GD.PushError($"Node {node_data["Filename"]} does not have Load() function");
                continue;
            }

            node.Call("Load", node_data);
        }

        return true;
    }

}
