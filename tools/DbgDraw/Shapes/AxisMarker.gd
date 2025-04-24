@tool
class_name DbgAxisMarker
extends DbgShape

@export var axis_aligned := true
@export var lead_length := 1.0 : set = _lead_length_set
func _lead_length_set(val: float) -> void:
    if val > 0:
        lead_length = val
    else:
        lead_length = 0.01

func _process(_delta: float) -> void:
    if axis_aligned:
        DbgDraw.AxisMarker(global_position, lead_length)
        
    else:
        DbgDraw.AxisMarker(global_position, lead_length, quaternion)
