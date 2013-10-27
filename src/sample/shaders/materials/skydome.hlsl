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
	float4 pos3D:  POSITION1;
	float3 normal: NORMAL0;
	float3 tangent: TANGENT0;
	float3 bitangent: BINORMAL0;
	float4 tex: TEXCOORD0;
};

float3 main(Pixel input) : SV_Target
{
	float3 r = normalize(input.pos3D.xyz);

	float phi = atan2(r.z, r.x);
	float theta = acos(r.y);

	return sky.Sample(texSampler, float2(phi / (2 * 3.14159265), theta / 3.14159265)) * brightness;
}