using System;
using System.IO;
using System.Drawing;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

using Insight;

namespace Sample
{
    /// <summary>
    /// A simple tonemapping algorithm.
    /// </summary>
    class ToneMapper : IDisposable
    {
        /// <summary>
        /// Gets the device associated with this ToneMapper.
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// Gets or sets the exposure level.
        /// </summary>
        public double Exposure { get; set; }

        /// <summary>
        /// Gets or set the gamma correction factor.
        /// </summary>
        public double Gamma { get; set; }

        /// <summary>
        /// The frame dimensions currently in effect.
        /// </summary>
        public Size Dimensions
        {
            get
            {
                return temporary.Dimensions;
            }

            set
            {
                InitializeResources(value);
            }
        }

        /// <summary>
        /// Temporary texture for the tonemapping pass.
        /// </summary>
        private GraphicsResource temporary;

        /// <summary>
        /// The required pixel shaders.
        /// </summary>
        private String averageShader, operateShader;

        /// <summary>
        /// Initializes graphics resources.
        /// </summary>
        /// <param name="dimensions">The frame dimensions.</param>
        private void InitializeResources(Size dimensions)
        {
            if (temporary != null) temporary.Dispose();
            temporary = new GraphicsResource(Device, dimensions, Format.R32G32B32A32_Float, true, true, true);
        }

        /// <summary>
        /// Creates a new ToneMapper instance.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="dimensions">The dimensions of the frame to tone-map.</param>
        /// <param name="exposure">Initial exposure level.</param>
        /// <param name="gamma">Initial gamma correction factor.</param>
        public ToneMapper(Device device, Size dimensions, double exposure, double gamma)
        {
            Device = device;
            Exposure = exposure;
            Dimensions = dimensions;
            
            Gamma = gamma;

            averageShader = File.ReadAllText(@"shaders/tonemapper/average.hlsl");
            operateShader = File.ReadAllText(@"shaders/tonemapper/operate.hlsl");
        }

        /// <summary>
        /// Tone-maps a texture into another. The target dimensions,
        /// source dimensions, and ToneMapper dimensions <b>must</b>
        /// be exactly the same for correct operation.
        /// </summary>
        /// <param name="context">The device context.</param>
        /// <param name="pass">A SurfacePass instance.</param>
        /// <param name="target">The render target.</param>
        /// <param name="source">The source texture.</param>
        public void ToneMap(DeviceContext context, SurfacePass pass, RenderTargetView target, ShaderResourceView source)
        {
            pass.Pass(context, averageShader, temporary.Dimensions, temporary.RTV, new[] { source }, null);

            context.GenerateMips(temporary.SRV);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<float>((float)(1.0 / Gamma));
            cbuffer.Write<float>((float)Exposure);
            cbuffer.Position = 0;

            pass.Pass(context, operateShader, temporary.Dimensions, target, new[] { temporary.SRV }, cbuffer);

            cbuffer.Dispose();
        }

        #region IDisposable

        /// <summary>
        /// Destroys this ToneMapper instance.
        /// </summary>
        ~ToneMapper()
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
                temporary.Dispose();
            }
        }

        #endregion
    }
}
