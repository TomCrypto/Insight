Texture2D<float3> sky : register(t0);

cbuffer camera : register(b0)
{
	float4x4 view;
	float4 camPos;
	float4 camDir;
};

cbuffer model : register(b1)
{
	float4x4 model;
};

cbuffer material : register(b2)
{
	float brightness;
};

SamplerState texSampler;

struct Pixel
{
	float4 pos : SV_POSITION;
	float4 pos3D:  COLOR0;
	float3 normal: NORMAL0;
	float3 tangent: TANGENT0;
	float3 bitangent: BINORMAL0;
	float4 tex: TEXCOORD0;
};

float3 main(Pixel input) : SV_Target
{
	float3 p = normalize(input.pos3D.xyz);

	float h = p.y; // estimated vertical location on skydome
	float l = p.x;

	float sunBrightness = (dot(p, normalize(float3(-0.5f, 0.8f, 0.9f))) > 0.9995f) ? 1 : 0;

	float2 uv = float2(p.xz * 0.5 + 0.5);

	float3 skySample = sky.Sample(texSampler, p.xz * 0.5 + 0.5) * brightness;

	//float3 skySample = float3(1, 0, 0) * input.pos3D.x;

	//return lerp(float3(1, 1, 1), float3(0.7f, 0.7f, 1), h) * brightness + sunBrightness * 17500;

	return skySample;// + sunBrightness * 17500;
}