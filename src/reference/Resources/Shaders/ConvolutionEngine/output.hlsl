#include <pass>

RWByteAddressBuffer buf : register(u1);

cbuffer constants : register(b0)
{
    uint w, h;
}

float main(PixelDefinition pixel) : SV_Target
{
	uint x = uint(pixel.tex.x * w);
	uint y = uint(pixel.tex.y * h);
    uint index = (y * w + x) << 3U;

    float2 c = asfloat(buf.Load2(index));
    //return sqrt(pow(c.x, 2) + pow(c.y, 2));
	return c.x; // theoretically correct, but may yield artifacts due to FP error?
}