#version 450

layout(location = 0) in vec2 position;
layout(location = 1) in vec3 color;

layout(push_constant) uniform Push{
	mat4 transform;
	vec2 offset;
	vec3 color;
} push;

void main() {
	mat2 asd;
	asd[0][0]= push.transform[0][0];
	asd[0][1]= push.transform[0][1];
	asd[1][0]= push.transform[1][0];
	asd[1][1]= push.transform[1][1];

	gl_Position = vec4(asd * position + push.offset, 0.0, 1.0);
}
