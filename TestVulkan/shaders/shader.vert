#version 450

layout(binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo;

layout(push_constant) uniform PushConsts {
	vec4 color;
	vec4 position;
} pushConsts;

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inTexCoord;

layout(location = 0) out vec3 fragColor;
layout(location = 1) out vec2 fragTexCoord;

void main() {
    //gl_Position = ubo.proj * ubo.view * ubo.model * vec4(inPosition, 1.0);

    vec3 locPos = vec3(ubo.model * vec4(inPosition, 1.0));
	vec3 worldPos = locPos + pushConsts.position.xyz;
	gl_Position =  ubo.proj * ubo.view * vec4(worldPos, 1.0);

    fragColor = inColor * pushConsts.color.rgb;	
    fragTexCoord = inTexCoord;
}
