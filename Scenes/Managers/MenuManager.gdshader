shader_type canvas_item;

uniform vec4 bg_color_1 : source_color = vec4(0.42, 0.55, 0.84, 1.0);
uniform vec4 bg_color_2 : source_color = vec4(0.35, 0.46, 0.76, 1.0);

uniform float wavy_speed = 10.5; 
uniform float wavy_amplitude = 0.015;

uniform float grid_scale = 4.0; 
uniform vec2 stretch = vec2(1.0, 1.0); 

void fragment() {
    vec2 uv = UV * stretch;
    
    vec2 distorted_uv = uv;
    
    distorted_uv.x += sin(uv.y * 15.0 + TIME * wavy_speed) * (wavy_amplitude * 0.1);
    distorted_uv.y += cos(uv.x * 15.0 + TIME * wavy_speed * 0.8) * wavy_amplitude;

    vec2 grid = floor(distorted_uv * grid_scale);
    
    float checker = mod(grid.x + grid.y, 2.0);
    vec3 final_color = mix(bg_color_1.rgb, bg_color_2.rgb, checker);

    COLOR = vec4(final_color, 1.0);
}