//#########################################################################//
// Shader Name:    operate.hlsl                                            //
// Shader Type:    SurfacePass shader                                      //
// Description:    Sample tone-mapper pass #2                              //
//#########################################################################//
//                                  INPUTS                                 //
//                                  ======                                 //
//                                                                         //
// t0: A high dynamic range RGBA texture with log-luminance alpha channel. //
//     This texture must have a 1x1 mipmap which can be sampled.           //
//                                                                         //
//                                 OUTPUTS                                 //
//                                 =======                                 //
//                                                                         //
// u0: An RGBA low dynamic range texture, tone-mapped and gamma-corrected, //
//     according to the provided shader parameters.                        //
//                                                                         //
//                                  PARAMS                                 //
//                                  ======                                 //
//                                                                         //
// b0: The exposure level and inverse of gamma correction factor.          //
//#########################################################################//

#include <pass>

#include "shaders/tonemapper/utils.hlsl"

Texture2D<float4> fTex                                     : register(t0);

cbuffer constants                                          : register(b0)
{
    float invgamma;
    float exposure;
};

float3 main(PixelDefinition pixel) : SV_Target
{
    uint w, h, m;

    fTex.GetDimensions(0, w, h, m);
	uint x = uint(pixel.tex.x * w);
	uint y = uint(pixel.tex.y * h);

	/* Obtain the log-average luminance from the 1x1 mip level. */
    float log_avg = exp(fTex.Load(int3(0, 0, m - 1)).w / (w * h));
    float key = exposure / log_avg;

	float3 rgb = fTex.Load(int3(x, y, 0)).xyz;
	float3 val = rgb * key / (1 + luminance(rgb) * key);

    return pow(val, invgamma);
}