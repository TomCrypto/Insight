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
	float4 kD, kS;
	float4 nS;
	float4 brightness;
	bool hasBumpMap;
}

SamplerState texSampler;

struct Pixel
{
	float4 pos : SV_POSITION;
	float4 pos3D:  TEXCOORD1;
	float3 normal: NORMAL0;
	float3 tangent: TANGENT0;
	float3 bitangent: BINORMAL0;
	float4 tex: TEXCOORD0;
};

static const int LIGHT_COUNT = 7;

static const float4 lights[LIGHT_COUNT] =
{
	float4(10.1, -3.4, 0, 2000),

	float4(10, -9, 0, 400),

	float4(5, -6, 3, 1000),
	float4(5, -6, -3, 1000),

	float4(-8, -10.7, -2.5, 300),
	float4(-8, -10.7, 2.5, 300),

	float4(-21, -5.4, 0.6, 1500)
};

float3 main(Pixel input) : SV_Target
{
	float3x3 tbn = float3x3(input.tangent, -input.bitangent, input.normal);

	float3 normal = input.normal.xyz;

	if (hasBumpMap)
	{
		float3 texNormal = bump.Sample(texSampler, input.tex.xy).xzy * 2 - 1;

		normal = mul(texNormal, tbn);
		normal = normalize(mul(float4(normal, 1), model)).xyz;
	}

	float3 c = float3(0, 0, 0);

	for (int t = 0; t < LIGHT_COUNT; ++t)
	{
		float3 lightPos = lights[t].xyz;

		float3 lightDir = (input.pos3D.xyz - lightPos);
		float falloff = length(lightDir);
		lightDir /= falloff;
		falloff = pow(falloff, 2);

		float lightBrightness = lights[t].w;

		float diffuse = max(0, dot(lightDir, normal) * lightBrightness) / falloff;

		float ambient = 0.1;

		float3 viewDir = normalize(camPos.xyz - input.pos3D.xyz);

		float3 reflected = normalize(reflect(lightDir, normal));
		float specular = lightBrightness * pow(max(0, dot(viewDir, reflected)), nS) / falloff;

		c += (kD * diffuse + kS * specular + ambient);
	}

	return color.Sample(texSampler, input.tex.xy).xyz * c;
}