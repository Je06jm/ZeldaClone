extends Node3D

func _ready() -> void:
    $AnimationPlayer.play("Walk")

func _process(_delta: float) -> void:
    var velocity: Vector3 = $AnimationPlayer.get_root_motion_position()
    position += velocity
