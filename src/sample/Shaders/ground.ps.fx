Texture2D color : register(t0);

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

cbuffer material : register(b2)
{
	float4 kD, kS;
	float4 nS;
	float4 brightness;
}

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
	return color.Sample(texSampler, input.tex.xy).xyz * 2;
}