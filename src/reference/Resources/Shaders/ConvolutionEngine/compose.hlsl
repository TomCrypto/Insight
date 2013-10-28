#include <pass>

Texture2D<float> rTex : register(t0);
Texture2D<float> gTex : register(t1);
Texture2D<float> bTex : register(t2);

SamplerState texSampler
{
    BorderColor = float4(0, 0, 0, 1);
    Filter = MIN_MAG_MIP_ANISOTROPIC;
    MaxAnisotropy = 16;
    AddressU = Border;
    AddressV = Border;
};

float3 main(PixelDefinition pixel) : SV_Target
{
    float2 offset = (pixel.tex + 0.5) / 2;

    float r = rTex.Sample(texSampler, offset);
    float g = gTex.Sample(texSampler, offset);
    float b = bTex.Sample(texSampler, offset);

    return float3(r, g, b);
}