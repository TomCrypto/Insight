cbuffer model : register(b0)
{
	float4x4 model;
};

cbuffer camera : register(b1)
{
	float4x4 view;
	float4 camPos;
	float4 camDir;
};

struct VertexIn
{
	float4 pos : POSITION;
	float4 normal: NORMAL0;
	float4 tex: TEXCOORD0;
};

struct PixelIn
{
	float4 pos : SV_POSITION;
	float4 pos3D:  COLOR0;
	float4 normal: TEXCOORD1;
	float4 tex: TEXCOORD0;
};

PixelIn main(VertexIn input)
{
   PixelIn output;
   output.pos = mul(mul(input.pos, model), view);
   output.pos3D = input.pos;
   output.normal = input.normal;
   output.tex = input.tex;
   
   return output;
}