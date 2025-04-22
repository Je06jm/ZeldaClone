shader_type spatial;
render_mode unshaded, depth_test_disabled, shadows_disabled;

uniform sampler2D mask: filter_linear;

uniform vec3 green_color: source_color;
uniform vec3 green_flash_color: source_color;
uniform vec3 red_bright_color: source_color;
uniform vec3 red_dark_color: source_color;
uniform vec3 base_color: source_color;

uniform float green_percentage;
uniform float red_percentage;

uniform float green_flash_amount;

uniform float red_flash_speed;

void vertex() {
	mat4 modified_model_view = VIEW_MATRIX * mat4(
        INV_VIEW_MATRIX[0],
        INV_VIEW_MATRIX[1],
        INV_VIEW_MATRIX[2],
        MODEL_MATRIX[3]
    );
    MODELVIEW_MATRIX = modified_model_view;
}

void fragment() {
	ALPHA = texture(mask, UV).r;

    if (ALPHA < 0.005) {
        discard;
    }

    vec2 coord = UV * 2.0 - 1.0;
    float p = atan(coord.x, coord.y);

    p /= PI;

    p = (p + 1.0) / 2.0;

    vec3 color;
    if (p <= green_percentage) {
        color = mix(green_color, green_flash_color, green_flash_amount);
    }
    else if (p <= red_percentage) {
        float amount = sin(TIME * red_flash_speed);
        amount = (amount + 1.0) / 2.0;
        color = mix(red_bright_color, red_dark_color, amount);
    }
    else {
        color = base_color;
    }

    ALBEDO = color;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
