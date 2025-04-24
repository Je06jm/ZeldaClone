@tool
class_name DbgCube
extends DbgShape

@export var size := Vector3.ONE
@export var color := Color.RED

func _process(_delta: float) -> void:
    DbgDraw.Cube(global_position, size, quaternion, color)
