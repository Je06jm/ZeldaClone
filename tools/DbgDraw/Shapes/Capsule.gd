@tool
class_name DbgCapsule
extends DbgShape

@export var color := Color.RED

@export var height := 1.0 : set = _height_set
func _height_set(val: float) -> void:
    if val > 0:
        height = val
    else:
        height = 0.01

var radius_type := 0 : set = _radius_type_set
func _radius_type_set(val: int) -> void:
    if radius_type != val:
        match val:
            0:
                radius = (radius_top + radius_bottom) / 2
            1:
                radius_top = radius
                radius_bottom = radius
                
    radius_type = val
    notify_property_list_changed()

var radius := 1.0 : set = _radius_set
func _radius_set(val: float) -> void:
    if val > 0:
        radius = val
    else:
        radius = 0.01

var radius_top := 1.0 : set = _radius_top_set
func _radius_top_set(val: float) -> void:
    if val > 0:
        radius_top = val
    else:
        radius_top = 0.01

var radius_bottom := 1.0 : set = _radius_bottom_set
func _radius_bottom_set(val: float) -> void:
    if val > 0:
        radius_bottom = val
    else:
        radius_bottom = 0.01

func _get_property_list() -> Array[Dictionary]:
    var list: Array[Dictionary] = []
    
    list.append({
        "name": &"radius_type",
        "type": TYPE_INT,
        "usage": PROPERTY_USAGE_DEFAULT,
        "hint": PROPERTY_HINT_ENUM,
        "hint_string": "Single Radius, Split Radius"
    })
    
    match radius_type:
        0:
            list.append({
                "name": &"radius",
                "type": TYPE_FLOAT,
                "usage": PROPERTY_USAGE_DEFAULT
            })
            
        1:
            list.append({
                "name": &"radius_top",
                "type": TYPE_FLOAT,
                "usage": PROPERTY_USAGE_DEFAULT
            })
            
            list.append({
                "name": &"radius_bottom",
                "type": TYPE_FLOAT,
                "usage": PROPERTY_USAGE_DEFAULT
            })
    
    return list

func _property_can_revert(property: StringName) -> bool:
    match property:
        &"radius_type":
            return true
            
        &"radius":
            return true
            
        &"radius_top":
            return true
            
        &"radius_bottom":
            return true
            
    return false

func _property_get_revert(property: StringName) -> Variant:
    match property:
        &"radius_type":
            return 0
            
        &"radius":
            return 1.0
            
        &"radius_top":
            return 1.0
            
        &"radius_bottom":
            return 1.0
            
    return null

func _process(_delta: float) -> void:
    match radius_type:
        0:
            DbgDraw.Capsule(global_transform, radius, height, color)
        
        1:
            DbgDraw.Capsule(global_transform, radius_top, radius_bottom, height, color)
