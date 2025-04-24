@tool
class_name DbgLine
extends DbgShape

@export var color := Color.RED

@export var target := Vector3.FORWARD

func _process(_delta: float) -> void:
    DbgDraw.Line(global_position, global_position + target, color)
