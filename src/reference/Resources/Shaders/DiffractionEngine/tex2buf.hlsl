#include <pass>

Texture2D<float>    gTex : register(t0);
RWByteAddressBuffer dest : register(u1);

float main(PixelDefinition pixel) : SV_Target
{
    uint w, h, m;

    gTex.GetDimensions(0, w, h, m);
	uint x = uint(pixel.tex.x * w);
	uint y = uint(pixel.tex.y * h);
    uint index = (y * w + x) << 3U;

    float2 value = float2(gTex.Load(int3(x, y, 0)), 0);
    dest.Store2(index, asuint(value));

	return 0;
}