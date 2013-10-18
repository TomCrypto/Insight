using System;

using System.Drawing;

using SharpDX.Direct3D11;

namespace Iridium
{
    /// <summary>
    /// Describes the quality at which diffraction effects are rendered.
    /// </summary>
    public enum RenderQuality
    {
        /// <summary>
        /// Low quality: aperture rendered at 256×256 resolution, convolution dimensions 512×512 pixels.
        /// Very fast, for low-end computers.
        /// </summary>
        Low,
        /// <summary>
        /// Medium quality: aperture rendered at 512×512 resolution, convolution dimensions 1024×1024 pixels.
        /// Good performance/accuracy balance.
        /// </summary>
        Medium,
        /// <summary>
        /// High quality: aperture rendered at 1024×1024 resolution, convolution dimensions 2048×2048 pixels.
        /// For high-end graphics cards only.
        /// </summary>
        High,
        /// <summary>
        /// Optimal quality: aperture rendered at 2048×2048 resolution, convolution dimensions 4096×4096 pixels.
        /// Intended for use in offline rendering.
        /// </summary>
        Optimal,
    }

    /// <summary>
    /// Describes some of the biological and optical properties of the eye
    /// being simulated, influencing the resulting diffraction effects.
    /// </summary>
    public struct OpticalProfile
    {
        // put stuff here
    }

    /// <summary>
    /// Provides configurable eye diffraction effects.
    /// </summary>
    public sealed class Iridium : IDisposable
    {
        private OpticalProfile profile;
        private RenderQuality quality;
        private Size dimensions;
        private double time;

        private GraphicsResource aperture;
        private GraphicsResource spectrum;

        private DiffractionEngine diffraction;
        private ConvolutionEngine convolution;

        /// <summary>
        /// The graphics device used by this Iridium instance.
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// A SurfacePass instance. You can use it if needed to save resources.
        /// </summary>
        public SurfacePass Pass { get; private set; }

        /// <summary>
        /// The optical profile currently used for rendering diffraction effects.
        /// </summary>
        public OpticalProfile Profile { get; set; }

        /// <summary>
        /// The render quality currently used for rendering diffraction effects.
        /// </summary>
        public RenderQuality Quality
        {
            get
            {
                return quality;
            }

            set
            {
                if (aperture != null) aperture.Dispose();
                if (spectrum != null) spectrum.Dispose();
                
                if (diffraction != null) diffraction.Dispose();
                if (convolution != null) convolution.Dispose();

                switch (quality = value)
                {
                    case RenderQuality.Low:
                        {
                            diffraction = new DiffractionEngine(Device, new Size(600, 600));
                            convolution = new ConvolutionEngine(Device, new Size(1024, 1024));

                            aperture = new GraphicsResource(Device, new Size(600, 600), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true, true);
                            spectrum = new GraphicsResource(Device, new Size(600, 600), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true, true);

                            break;
                        }
                    case RenderQuality.Medium:
                        {
                            diffraction = new DiffractionEngine(Device, new Size(512, 512));
                            // convolution = ...
                            aperture = new GraphicsResource(Device, new Size(512, 512), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true);
                            spectrum = new GraphicsResource(Device, new Size(512, 512), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true);

                            break;
                        }
                    case RenderQuality.High:
                        {
                            diffraction = new DiffractionEngine(Device, new Size(1024, 1024));
                            // convolution = ...

                            aperture = new GraphicsResource(Device, new Size(1024, 1024), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true);
                            spectrum = new GraphicsResource(Device, new Size(1024, 1024), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true);

                            break;
                        }
                    case RenderQuality.Optimal:
                        {
                            diffraction = new DiffractionEngine(Device, new Size(2048, 2048));
                            // convolution = ...

                            aperture = new GraphicsResource(Device, new Size(2048, 2048), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true);
                            spectrum = new GraphicsResource(Device, new Size(2048, 2048), SharpDX.DXGI.Format.R32G32B32A32_Float, true, true);

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// The dimensions of the surface to which to add diffraction effects to.
        /// </summary>
        public Size Dimensions
        {
            get
            {
                return dimensions;
            }

            set
            {
                if (value != null) dimensions = value;
                else throw new ArgumentNullException("The surface dimensions cannot be null.");
            }
        }

        /// <summary>
        /// Creates an Iridium instance with custom settings. The graphics device
        /// will be reused, but will not be disposed of at instance destruction.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="dimensions">The target surface dimensions.</param>
        /// <param name="quality">The required render quality.</param>
        /// <param name="profile">The desired optical profile.</param>
        public Iridium(Device device, Size dimensions, RenderQuality quality, OpticalProfile profile)
        {
            Device = device;            /* Store the device. */
            Quality = quality;          /* Validate quality. */
            Profile = profile;          /* Use lens profile. */
            Dimensions = dimensions;    /* Check dimensions. */

            Pass = new SurfacePass(device);
        }

        /// <summary>
        /// Superimposes eye diffraction effects on a texture, created with render target
        /// and shader resource bind flags. The texture should have a high dynamic range.
        /// </summary>
        /// <param name="surface">The source and destination texture.</param>
        /// <param name="dt">The time elapsed since the last call, in seconds.</param>
        public void Augment(Texture2D surface, double dt = 0)
        {
            RenderTargetView rtv = new RenderTargetView(Device, surface);
            ShaderResourceView srv = new ShaderResourceView(Device, surface);

            // TODO: this is where the aperture is dynamically generated
            Device.ImmediateContext.CopyResource(surface, aperture.Resource);

            //diffraction.Diffract(Device, Pass, spectrum.RT, aperture.SRV);
            //diffraction.Diffract(Device, Pass, rtv, aperture.SRV, 1);
            diffraction.Diffract(Device, Pass, rtv, aperture.SRV, 1 + 1 * (0.5 * Math.Sin(time) + 0.5));

            // TODO: this is where the spectrum is convolved with the surface
            //Device.ImmediateContext.CopyResource(spectrum.Resource, surface);

            //convolution.Convolve(Device, Pass, rtv, spectrum.SRV, srv);

            rtv.Dispose();
            srv.Dispose();

            time += dt;
        }

        #region IDisposable

        /// <summary>
        /// Destroys this Iridium instance.
        /// </summary>
        ~Iridium()
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
                convolution.Dispose();
                diffraction.Dispose();

                aperture.Dispose();
                spectrum.Dispose();
            }
        }

        #endregion
    }
}
