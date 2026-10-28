
shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform float bend_1 : hint_range(-2.0, 2.0) = 0.8;
uniform float bend_2 : hint_range(-2.0, 2.0) = -0.8;

uniform vec4 body_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);

void vertex() {
    // Original vertex position
    vec3 p = VERTEX;

    // First bend: lower part
    float lower = clamp(-p.y, 0.0, 1.5);
    float angle1 = bend_1 * lower;

    // Second bend: upper part
    float upper = clamp(p.y, 0.0, 1.5);
    float angle2 = bend_2 * upper;

    // Smooth horizontal displacement
    p.x += sin(angle1) * lower;
    p.x += sin(angle2) * upper;

    VERTEX = p;
}

void fragment() {
    ALBEDO = body_color.rgb;
    ROUGHNESS = 0.85;
}
