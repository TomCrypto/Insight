#include <pass>

RWByteAddressBuffer bufA : register(u1);
RWByteAddressBuffer bufB : register(u2);

cbuffer constants : register(b0)
{
    uint w, h;
}

float2 complex_mul(float2 a, float2 b)
{
    return float2(a.x * b.x - a.y * b.y,
                    a.y * b.x + a.x * b.y);
}

float main(PixelDefinition pixel) : SV_Target
{
	uint x = uint(pixel.tex.x * w);
	uint y = uint(pixel.tex.y * h);
    uint index = (y * w + x) << 3U;

    float2 valA = asfloat(bufA.Load2(index));
    float2 valB = asfloat(bufB.Load2(index));

    bufA.Store2(index, asuint(complex_mul(valA, valB)));

    return 0;
}