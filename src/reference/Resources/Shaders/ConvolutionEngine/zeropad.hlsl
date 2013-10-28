#include <pass>

Texture2D<float3>   gTex : register(t0);
RWByteAddressBuffer dest : register(u1);

cbuffer constants : register(b0)
{
    uint w, h;
}

float main(PixelDefinition pixel) : SV_Target
{
	uint x = uint(pixel.tex.x * w);
	uint y = uint(pixel.tex.y * h);
    uint index = (y * w + x) << 3U;

	float intensity = gTex.Load(int3(x, y, 0)).CHANNEL;
    dest.Store2(index, asuint(float2(intensity, 0)));

    return 0;
}