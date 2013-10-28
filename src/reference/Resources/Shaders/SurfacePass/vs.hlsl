#include <pass>
 
PixelDefinition main(uint id : SV_VertexID)
{
    PixelDefinition output;

    output.tex = float2((id << 1) & 2, id & 2);
    output.pos = float4(output.tex * float2(2, -2) + float2(-1, 1), 0, 1);

    return output;
}