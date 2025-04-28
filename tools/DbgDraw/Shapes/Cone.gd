@tool
class_name DbgCone
extends DbgShape

@export var color := Color.RED
@export var radius := 1.0
@export var length := 1.0

func _process(_delta: float) -> void:
    DbgDraw.Cone(global_transform, radius, length, color);
