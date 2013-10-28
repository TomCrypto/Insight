#include <pass>

RWByteAddressBuffer buffer : register(u1);

cbuffer constants : register(b0)
{
    uint w, h;
}

float main(PixelDefinition pixel) : SV_Target
{
	uint x = (uint(pixel.tex.x * w) + w / 2) % w;
	uint y = (uint(pixel.tex.y * h) + h / 2) % h;
    uint index = (y * w + x) << 3U;

    float2 value = asfloat(buffer.Load2(index));
	return pow(value.x, 2) + pow(value.y, 2);
}