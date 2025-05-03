extends Node3D

func select_target(bodies: Array[Area3D], node: Node3D, must_be_on_screen: bool) -> Node3D:
    var selected_body: Node3D = null
    var selected_dot := -1.0
    
    for body: LookTarget in bodies:
        if not body.on_screen and must_be_on_screen:
            continue
            
        var dir := node.to_local(body.global_position).normalized()
        var dot := Vector3.FORWARD.dot(dir)
        
        if dot > selected_dot:
            selected_body = body
            selected_dot = dot
        
    return selected_body

func _process(_delta: float) -> void:
    var cam_targets: Array[Area3D] = $CameraHing/Offset/Camera3D/TargetSelect.get_overlapping_areas()
    var char_targets: Array[Area3D] = $Character/TargetSelect.get_overlapping_areas()
    
    var target: Node3D = null
    
    if cam_targets.is_empty():
        if not char_targets.is_empty():
            target = select_target(char_targets, $Character/TargetSelect, false)
            
    else:
        target = select_target(cam_targets, $CameraHing/Offset/Camera3D/TargetSelect, true)
        
    if not Input.is_action_pressed("shield"):
        $Character.target_node = target
        $CameraHing.target_node = target
        
    var pos3d: Vector3 = $Character.global_position
    var pos2d: Vector2 = $CameraHing/Offset/Camera3D.unproject_position(pos3d)
    
    $Stamina.global_position = pos2d

    if Input.is_action_just_pressed("dbg_save"):
        SaveGame.Save(false)

    if Input.is_action_just_pressed("dbg_load"):
        SaveGame.Load(0)
