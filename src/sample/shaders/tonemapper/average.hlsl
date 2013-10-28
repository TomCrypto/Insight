//#########################################################################//
// Shader Name:    average.hlsl                                            //
// Shader Type:    SurfacePass shader                                      //
// Description:    Sample tone-mapper pass #1                              //
//#########################################################################//
//                                  INPUTS                                 //
//                                  ======                                 //
//                                                                         //
// t0: A high dynamic range RGB texture to be tone-mapped.                 //
//                                                                         //
//                                 OUTPUTS                                 //
//                                 =======                                 //
//                                                                         //
// u0: An RGBA texture, with the alpha channel equal to the logarithm of   //
//     the luminance of the corresponding pixel, in the CIE color space.   //
//#########################################################################//

#include <pass>

#include "shaders/tonemapper/utils.hlsl"

Texture2D<float3> fTex                                     : register(t0);

static const float EPSILON = 1e-6f;

float4 main(PixelDefinition pixel) : SV_Target
{
    uint w, h, m;

    fTex.GetDimensions(0, w, h, m);
	uint x = uint(pixel.tex.x * w);
	uint y = uint(pixel.tex.y * h);

    float3 rgb = fTex.Load(int3(x, y, 0));
	float logL = log(luminance(rgb));

    return float4(rgb, max(0, logL + EPSILON));
}