Texture2D color : register(t1);

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
	float4 col = color.Sample(texSampler, input.tex.xy);

	float3 lightPos = float3(0, 2, 0);

	float3 lightDir = normalize(lightPos - input.pos3D.xyz);

	// sample texture as usual
	float3 diffuse = kD.xyz * max(0, dot(lightDir, input.normal.xyz));
	
	float3 R = normalize(reflect(lightDir, input.normal.xyz));

	float3 specular = kS.xyz * pow(max(0, dot(normalize(-camDir.xyz), R)), nS);

	return col.xyz * 0.05f * brightness.x * (diffuse + specular) * 0.1f * 35;
}