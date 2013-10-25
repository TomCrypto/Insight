Texture2D sky : register(t0);
Texture2D color : register(t1);

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
	/*float4 kD, kS;
	float4 nS;
	float4 brightness;*/

	float albedo;
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
	float3 dir = normalize(input.pos3D.xyz - mul(camPos, model));

	float3 r = reflect(dir, input.normal.xyz);

	float3 p = normalize(r);

	//return color.Sample(texSampler, input.tex.xy).xyz * albedo;

	return color.Sample(texSampler, input.tex.xy).xyz * albedo + sky.Sample(texSampler, p.xz * 0.5 + 0.5).xyz * 0.3f;
}