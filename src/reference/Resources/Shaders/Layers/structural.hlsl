#include <pass>

cbuffer constants : register(b0)
{
	float size;
    float glare;
};

float main(PixelDefinition pixel) : SV_Target
{
    float2 p = pixel.tex * 2 - 1;

    float f = 1 / (1 - glare);

    if (pow(p.x, 2) + pow(f * p.y, 2) < pow(size, 2)) return 1;
    else return 0;
}