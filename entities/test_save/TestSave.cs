using Godot;
using System;

public partial class TestSave : RigidBody3D
{

    public GameSaveData Save() {
        return new GameSaveData(){
            {"PosX", Position.X},
            {"PosY", Position.Y},
            {"PosZ", Position.Z},
            {"LinX", LinearVelocity.X},
            {"LinY", LinearVelocity.Y},
            {"LinZ", LinearVelocity.Z},
            {"AngX", AngularVelocity.X},
            {"AngY", AngularVelocity.Y},
            {"AngZ", AngularVelocity.Z},
            {"Sleeping", Sleeping}
        };
    }

    public void Load(GameSaveData data) {
        Position = new Vector3(
            (float)data["PosX"],
            (float)data["PosY"],
            (float)data["PosZ"]
        );

        LinearVelocity = new Vector3(
            (float)data["LinX"],
            (float)data["LinY"],
            (float)data["LinZ"]
        );

        AngularVelocity = new Vector3(
            (float)data["AngX"],
            (float)data["AngY"],
            (float)data["AngZ"]
        );

        Sleeping = (bool)data["Sleeping"];
    }

}
