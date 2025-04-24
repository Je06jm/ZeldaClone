@tool
class_name DbgShape
extends Node3D

@export_enum("Editor Only", "Game Only", "Editor and Game") var visibility := 2 : set = _visibility_set
func _visibility_set(val: int) -> void:
    visibility = val
    
    match val:
        0:
            if Engine.is_editor_hint():
                set_process(true)
            else:
                set_process(false)
        1:
            if Engine.is_editor_hint():
                set_process(false)
            else:
                set_process(true)
        2:
            set_process(true)
            
func _ready() -> void:
    visibility = visibility
