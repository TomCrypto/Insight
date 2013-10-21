cbuffer stuff
{
	float4x4 viewproj;
	float4 cameraPos;
	float4 eye;
};

struct VertexIn
{
	float4 pos : POSITION;
	float4 color: COLOR0;
};

struct PixelIn
{
	float4 pos : SV_POSITION;
	float4 color: COLOR0;
};

PixelIn main(VertexIn input)
{
   PixelIn output;
   output.pos = mul(float4(input.pos.xyz, 1), viewproj);
   //output.pos = input.pos;
   output.color = input.color;
   
   return output;
}