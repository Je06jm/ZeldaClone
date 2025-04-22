extends Control

@onready var controller_slider := $Settings/VBoxContainer/ScrollContainer/MarginContainer/VBoxContainer/GridContainer/ControllerSlider
@onready var controller_value := $Settings/VBoxContainer/ScrollContainer/MarginContainer/VBoxContainer/GridContainer/ControllerValue

@onready var mouse_slider := $Settings/VBoxContainer/ScrollContainer/MarginContainer/VBoxContainer/GridContainer/MouseSlider
@onready var mouse_value := $Settings/VBoxContainer/ScrollContainer/MarginContainer/VBoxContainer/GridContainer/MouseValue

@onready var graphics_menu := $Settings/VBoxContainer/ScrollContainer/MarginContainer/VBoxContainer/Graphics
@onready var fsr_menu := $Settings/VBoxContainer/ScrollContainer/MarginContainer/VBoxContainer/FSR
@onready var scaling_menu := $Settings/VBoxContainer/ScrollContainer/MarginContainer/VBoxContainer/Scaling

var fullscreen := false

@export var player: Player

var graphics_setting := 0
var fsr_setting := 0
var scaling_setting := 0

var show_ui := false : set = _show_ui_set
func _show_ui_set(val: bool) -> void:
    show_ui = val
    
    visible = val
    
    get_tree().paused = val
    $Main.visible = true
    $Settings.visible = false
    
    if val:
        Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
    else:
        Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
    
func _ready() -> void:
    var graphics_popup: PopupMenu = graphics_menu.get_popup()
    var fsr_popup: PopupMenu = fsr_menu.get_popup()
    var scaling_popup: PopupMenu = scaling_menu.get_popup()
    
    graphics_popup.id_pressed.connect(func (id: int) -> void:
        graphics_setting = id    
    )
    
    fsr_popup.id_pressed.connect(func (id: int) -> void:
        fsr_setting = id    
    )
    
    scaling_popup.id_pressed.connect(func (id: int) -> void:
        scaling_setting = id    
    )
    
    show_ui = false

func _process(_delta: float) -> void:
    if Input.is_action_just_pressed("ui_cancel") and not show_ui:
        show_ui = true

func _on_resume_pressed() -> void:
    show_ui = false

func _on_settings_pressed() -> void:
    $Main.visible = false
    $Settings.visible = true

func _on_quit_pressed() -> void:
    get_tree().quit()


func _on_controller_slider_drag_ended(value_changed: bool) -> void:
    controller_value.text = str(controller_slider.value)

func _on_controller_value_text_submitted(new_text: String) -> void:
    if not new_text.is_valid_float():
        return
        
    var val: float = snapped(float(new_text), 0.01);
    
    val = clamp(val, controller_slider.min_value, controller_slider.max_value)
    
    controller_slider.value = val
    
    controller_value.text = str(val)


func _on_mouse_slider_drag_ended(value_changed: bool) -> void:
    mouse_value.text = str(mouse_slider.value)

func _on_mouse_value_text_submitted(new_text: String) -> void:
    if not new_text.is_valid_float():
        return
        
    var val: float = snapped(float(new_text), 0.01)
    
    val = clamp(val, mouse_slider.min_value, mouse_slider.max_value)
    
    mouse_slider.value = val
    
    mouse_value.text = str(val)


func _on_done_pressed() -> void:
    if player:
        player.GAMEPAD_LOOK_SPEED = controller_slider.value
        player.MOUSE_LOOK_SPEED = mouse_slider.value
        
    var viewport := get_viewport()
    
    match fsr_setting:
        0:
            viewport.scaling_3d_mode = Viewport.SCALING_3D_MODE_BILINEAR
            viewport.scaling_3d_scale = 1.0
            
        1:
            viewport.scaling_3d_mode = Viewport.SCALING_3D_MODE_FSR
            
        2:
            viewport.scaling_3d_mode = Viewport.SCALING_3D_MODE_FSR2
            
    if fsr_setting != 0:
        match scaling_setting:
            0:
                viewport.scaling_3d_scale = 0.5
            1:
                viewport.scaling_3d_scale = 0.65
            2:
                viewport.scaling_3d_scale = 0.75
            3:
                viewport.scaling_3d_scale = 0.9
                
    if fullscreen:
        var size := DisplayServer.window_get_max_size()
        DisplayServer.window_set_size(size)
        DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN)
    else:
        DisplayServer.window_set_size(Vector2i(1152, 648))
        DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
                
    $Settings.visible = false
    $Main.visible = true


func _on_fullscreen_toggled(toggled_on: bool) -> void:
    fullscreen = toggled_on
