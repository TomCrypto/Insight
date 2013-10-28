#include <pass>

Texture2D<float3> gTex : register(u1);

SamplerState texSampler
{
    BorderColor = float4(0, 0, 0, 1);
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Border;
    AddressV = Border;
};

float3 main(PixelDefinition pixel) : SV_Target
{
	return gTex.Sample(texSampler, pixel.tex);
}