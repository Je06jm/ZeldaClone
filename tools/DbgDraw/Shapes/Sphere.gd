@tool
class_name DbgSphere
extends DbgShape

@export var color := Color.RED
@export var radius := 1.0

func _process(_delta: float) -> void:
    DbgDraw.Sphere(global_transform, radius, color)
