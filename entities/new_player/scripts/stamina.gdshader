shader_type canvas_item;

uniform float outer_radius = 1.0;
uniform float inner_radius = 0.5;

uniform float available_percentage = 0.0;
uniform float consume_percentage = 0.0;
uniform float default_percentage = 1.0;
uniform vec3 available_color: source_color;
uniform vec3 consume_color: source_color;
uniform vec3 default_color: source_color;

void fragment() {
    vec2 coord = UV * 2.0 - 1.0;

    float d = length(coord);

    if ((d >= inner_radius) && (d <= outer_radius)) {
        float p = atan(coord.x, coord.y);

        p /= PI;

        p = (p + 1.0) / 2.0;

        if (p <= available_percentage) {
            COLOR = vec4(available_color, 1.0f);
        }
        else if (p <= consume_percentage) {
            COLOR = vec4(consume_color, 1.0);
        }
        else if (p <= default_percentage) {
            COLOR = vec4(default_color, 1.0);
        }
        else {
            COLOR = vec4(0.0);
        }
    }
    else
        COLOR = vec4(0.0);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
