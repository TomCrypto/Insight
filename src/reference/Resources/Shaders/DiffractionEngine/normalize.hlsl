#include <pass>

Texture2D<float3> spectrum : register(t0);

cbuffer constants : register(b0)
{
    float z; // observation plane distance
}

static const float threshold = 1e-9f; // for removing ultra-low-amplitude background noise

float3 main(PixelDefinition pixel) : SV_Target
{
    uint w, h, maxMips;
    spectrum.GetDimensions(0, w, h, maxMips);

    uint x = uint(pixel.tex.x * w);
    uint y = uint(pixel.tex.y * h);

    float3 norm = spectrum.Load(int3(0, 0, maxMips - 1)) * (w * h);
    return max(0, spectrum.Load(int3(x, y, 0)) / norm - threshold) * pow(z, -4);
}