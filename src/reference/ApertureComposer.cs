using System;

using SharpDX;
using SharpDX.Direct3D11;

namespace Insight
{
    /// <summary>
    /// This class generates apertures from an optical
    /// profile by composing it from multiple aperture
    /// elements blocking or transmitting light across
    /// the lens.
    /// </summary>
    class ApertureComposer : IDisposable
    {
        /// <summary>
        /// Creates a new ApertureComposer instance.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        public ApertureComposer(Device device)
        {
            // do any setup work here
        }

        /// <summary>
        /// Composes an aperture.
        /// </summary>
        /// <param name="context">The device context.</param>
        /// <param name="output">The output render target.</param>
        /// <param name="profile">The optical profile to use.</param>
        /// <param name="pass">A SurfacePass instance to use.</param>
        public void Compose(DeviceContext context, GraphicsResource output, OpticalProfile profile, SurfacePass pass)
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
        
        #region IDisposable

        /// <summary>
        /// Destroys this ApertureComposer instance.
        /// </summary>
        ~ApertureComposer()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes of all used resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO add stuff here
            }
        }

        #endregion
    }
}
