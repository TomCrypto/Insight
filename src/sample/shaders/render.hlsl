//#########################################################################//
// Shader Name:    render.hlsl                                             //
// Shader Type:    Scene Vertex Shader                                     //
// Description:    Vertex shader for all meshes                            //
//#########################################################################//
//                                  INPUTS                                 //
//                                  ======                                 //
//                                                                         //
// Vertices with position, normal, and texture coordinates.                //
//                                                                         //
//                                  PARAMS                                 //
//                                  ======                                 //
//                                                                         //
// b0: The model buffer, with a model->world matrix.                       //
// b1: The camera buffer, with the necessary matrices.                     //
//#########################################################################//

cbuffer camera                                             : register(b0)
{
	float4x4 view;
	float4 camPos;
	float4 camDir;
};

cbuffer model                                              : register(b1)
{
	float4x4 model;
};

struct Vertex
{
	float4 pos : POSITION;
	float4 normal: NORMAL0;
	float4 tangent: TANGENT0;
	float4 bitangent: BINORMAL0;
	float4 tex: TEXCOORD0;
};

struct Pixel
{
	float4 pos : SV_POSITION;
	float4 pos3D:  COLOR0;
	float3 normal: NORMAL0;
	float3 tangent: TANGENT0;
	float3 bitangent: BINORMAL0;
	float4 tex: TEXCOORD0;
};

Pixel main(Vertex input)
{
   Pixel output;

   output.pos3D = mul(input.pos, model);
   output.pos   = mul(output.pos3D, view);

   output.normal = input.normal;
   output.tangent = input.tangent;
   output.bitangent = input.bitangent;

   output.tex = input.tex;
   
   return output;
}