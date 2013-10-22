cbuffer stuff
{
	float4x4 viewproj;
	float4 cameraPos;
	float4 eye;
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
   output.pos = mul(input.pos, viewproj);
   output.pos3D = input.pos;
   output.normal = input.normal;
   output.tex = input.tex;
   
   return output;
}