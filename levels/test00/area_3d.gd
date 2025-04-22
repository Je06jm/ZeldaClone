extends Area3D

var played_cutscene := false

func _on_body_entered(_body: Node3D) -> void:
    if not played_cutscene:
        $AnimationPlayer.play("cutscene")
        played_cutscene = true
