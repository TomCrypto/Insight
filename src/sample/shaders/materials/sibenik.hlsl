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
	float4 kD, kS;
	float4 nS;
	float4 brightness;
}

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
	float4 col = color.Sample(texSampler, input.tex.xy);

	float3 lightPos = float3(0, 2, 0);

	float3 lightDir = normalize(lightPos - input.pos3D.xyz);

	// sample texture as usual
	float3 diffuse = kD.xyz * max(0, dot(lightDir, input.normal.xyz));
	
	float3 R = normalize(reflect(lightDir, input.normal.xyz));

	float3 specular = kS.xyz * pow(max(0, dot(normalize(-camDir.xyz), R)), nS);

	return col.xyz * 0.05f * brightness.x * (diffuse + specular) * 0.1f * 35;
}