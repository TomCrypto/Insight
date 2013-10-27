using System;

using SharpDX;
using SharpDX.Direct3D11;

namespace Insight.Layers
{
    /// <summary>
    /// This is the simplest layer, which simply describes
    /// the circular shape of an eye aperture - optionally
    /// with glare.
    /// </summary>
    class StructuralLayer : ApertureLayer
    {
        public override void ApplyLayer(DeviceContext context, GraphicsResource output, OpticalProfile profile, SurfacePass pass)
        {
            DataStream cbuffer = new DataStream(4, true, true);
            cbuffer.Write<float>((float)profile.Glare);
            cbuffer.Position = 0;

            pass.Pass(context, @"
            #include <surface_pass>

            cbuffer constants : register(b0)
            {
                float glare;
            };

            float main(PixelDefinition pixel) : SV_Target
            {
                float2 p = pixel.tex * 2 - 1;

                float f = 1 / (1 - glare);

                if (pow(p.x, 2) + pow(f * p.y, 2) < 0.35 * 0.35) return 1;
                else return 0;
            }

            ", output.Dimensions, output.RTV, null, cbuffer);

            cbuffer.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            /* Nothing to dispose of. */
        }
    }
}
