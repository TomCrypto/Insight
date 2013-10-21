cbuffer info
{
	float4x4 viewproj;
	float4 cameraPos;
	float4 eye;
}

struct PixelIn
{
	float4 pos : SV_POSITION;
	float4 color: COLOR0;
};

float4 main(PixelIn input) : SV_Target
{
	return float4(input.color.xyz, 1);
}