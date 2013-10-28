using System;
using System.Text;

using SharpDX;
using SharpDX.Direct3D11;

namespace Insight.Layers
{
    /// <summary>
    /// This is the simplest layer, which simply describes
    /// the circular shape of an eye aperture - optionally
    /// with glare.
    /// </summary>
    internal class StructuralLayer : ApertureLayer
    {
        public override void ApplyLayer(DeviceContext context, GraphicsResource output, OpticalProfile profile, SurfacePass pass)
        {
            DataStream cbuffer = new DataStream(4, true, true);
            cbuffer.Write<float>((float)profile.Glare);
            cbuffer.Position = 0;

            pass.Pass(context, Encoding.ASCII.GetString(Resources.Structural), output.Dimensions, output.RTV, null, cbuffer);

            cbuffer.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            /* Nothing to dispose of. */
        }
    }
}
