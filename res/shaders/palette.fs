#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

// Input uniform values
uniform sampler2D texture0;
uniform sampler2D palette;
uniform float opacity;
uniform int row_count;
uniform int row;

// Output fragment color
out vec4 finalColor;

void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    float row_count_f = float(row_count);
    finalColor = texture(palette, vec2(texel.r + 1.0/15.0 * 0.5, row/row_count_f + 1.0/row_count_f * 0.5));
    finalColor.a = texel.a * opacity;
}