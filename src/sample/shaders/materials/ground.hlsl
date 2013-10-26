Texture2D sky : register(t0);
Texture2D color : register(t1);
Texture2D bump : register(t2);

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
	float albedo;
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
	float3x3 tbn = float3x3(input.tangent, -input.bitangent, input.normal);

	float3 texNormal = bump.Sample(texSampler, input.tex.xy).xzy * 2 - 1;

	float3 normal = mul(texNormal, tbn);
	normal = normalize(mul(float4(normal, 1), model)).xyz;

	float3 lightPos = float3(-50, 0, 0);

	float3 lightDir = (input.pos3D.xyz - lightPos);
	float falloff = length(lightDir);
	lightDir /= falloff;
	falloff = pow(falloff, 2);

	float lightBrightness = 10000;

	float diffuse = 2 * max(0, dot(lightDir, normal) * lightBrightness) / falloff;

	float ambient = 0.10;

	float3 viewDir = normalize(camPos.xyz - input.pos3D.xyz);

	float3 reflected = normalize(reflect(lightDir, normal));
	float specular = 10 * lightBrightness * pow(max(0, dot(viewDir, reflected)), 256) / falloff;



	return color.Sample(texSampler, input.tex.xy).xyz * (diffuse + specular + ambient);
}