//#########################################################################//
// Shader Name:    utils.hlsl                                              //
// Shader Type:    Utility HLSL library                                    //
// Description:    Provides some utility functions to the tone-mapper      //
//#########################################################################//

/* CIE luminance formula */
float luminance(float3 rgb)
{
	return dot(rgb, float3(0.2126f, 0.7152f, 0.0722f));
}