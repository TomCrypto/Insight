cbuffer model : register(b0)
{
	float4x4 model;
};

cbuffer camera : register(b1)
{
	float4x4 view;
	float4 camPos;
	float4 camDir;
};

SamplerState texSampler;

struct PixelIn
{
	float4 pos : SV_POSITION;
	float4 pos3D:  COLOR0;
	float4 normal: TEXCOORD1;
	float4 tex: TEXCOORD0;
};

float3 main(PixelIn input) : SV_Target
{
	float3 p = normalize(input.pos3D.xyz);

	float h = p.y; // estimated vertical location on skydome
	float l = p.x;

	float brightness = 250;

	float sunSize = 10;

	float sunBrightness = (dot(p, normalize(float3(-1.6f, 0.8f, 0.9f))) > 0.9997f) ? 1 : 0;

	return lerp(float3(1, 1, 1), float3(0.7f, 0.7f, 1), h) * brightness + sunBrightness * 10000;
}