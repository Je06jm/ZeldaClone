shader_type sky;

uniform sampler2D sky_color: repeat_disable;
uniform sampler2D stars_texture;

uniform float day_amount;

uniform vec3 sun_dir;
uniform vec3 sun_color: source_color;

void sky() {
    vec3 sky = texture(sky_color, vec2(day_amount, 0.0f)).rgb;
    
    float d = dot(EYEDIR, -sun_dir);
    if (d >= 0.995) {
        d = 1.0;
    }
    else {
        d = 0.0;
    }
    
    COLOR = mix(sky, sun_color, d);
}