using System;

using System.Drawing;

using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

namespace Insight
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
        Low = 1,

        /// <summary>
        /// Medium quality: aperture rendered at 512×512 resolution, convolution dimensions 1024×1024 pixels.
        /// Good performance/accuracy balance.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High quality: aperture rendered at 1024×1024 resolution, convolution dimensions 2048×2048 pixels.
        /// For high-end graphics cards only.
        /// </summary>
        High = 3,

        /// <summary>
        /// Optimal quality: aperture rendered at 2048×2048 resolution, convolution dimensions 4096×4096 pixels.
        /// Intended for use in offline rendering.
        /// </summary>
        Optimal = 4,
    }

    /// <summary>
    /// Describes some of the biological and optical properties of the eye
    /// being simulated, influencing the resulting diffraction effects.
    /// </summary>
    public class OpticalProfile
    {
        /// <summary>
        /// Gets or sets the f-number of the aperture.
        /// </summary>
        public double FNumber { get; set; }

        // TODO: add a default constructor which sets sensible initial values here
    }

    /// <summary>
    /// Provides configurable eye diffraction effects.
    /// </summary>
    public sealed class EyeDiffraction : IDisposable
    {
        private RenderQuality quality;

        private GraphicsResource aperture;
        private GraphicsResource spectrum;

        private DiffractionEngine diffraction;
        private ConvolutionEngine convolution;

        /// <summary>
        /// The graphics device used by this LensFlare instance.
        /// </summary>
        public Device Device { get; private set; }

        /// <summary>
        /// The graphics context used by this LensFlare instance.
        /// </summary>
        public DeviceContext Context { get; private set; }

        /// <summary>
        /// A SurfacePass instance. You can use it if needed to save resources.
        /// </summary>
        public SurfacePass Pass { get; private set; }

        /// <summary>
        /// The optical profile currently used for rendering diffraction effects.
        /// </summary>
        public OpticalProfile Profile { get; set; }

        /// <summary>
        /// The current simulation time.
        /// </summary>
        public double Time { get; set; }

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
                if ((value != quality) || (Profile == null))
                {
                    if (aperture != null) aperture.Dispose();
                    if (spectrum != null) spectrum.Dispose();

                    if (diffraction != null) diffraction.Dispose();
                    if (convolution != null) convolution.Dispose();

                    diffraction = new DiffractionEngine(Device, Context, DiffractionSize(value));
                    convolution = new ConvolutionEngine(Device, Context, ConvolutionSize(value));

                    aperture = new GraphicsResource(Device, DiffractionSize(value), Format.R32G32B32A32_Float, true, true, true);
                    spectrum = new GraphicsResource(Device, DiffractionSize(value), Format.R32G32B32A32_Float, true, true);

                    quality = value;

                    LoadAperture("aperture" + (DiffractionSize(quality).Width) + ".png"); // TODO: temporary
                }
            }
        }

        private Size DiffractionSize(RenderQuality quality)
        {
            switch (quality)
            {
                case RenderQuality.Low:             return new Size( 256,  256);
                case RenderQuality.Medium:          return new Size( 512,  512);
                case RenderQuality.High:            return new Size(1024, 1024);
                case RenderQuality.Optimal:         return new Size(2048, 2048);
                default: throw new ArgumentException("Unknown render quality.");
            }
        }

        private Size ConvolutionSize(RenderQuality quality)
        {
            return new Size(DiffractionSize(quality).Width  * 2,
                            DiffractionSize(quality).Height * 2);
        }

        /// <summary>
        /// Creates a LensFlare instance with custom settings. The graphics device
        /// will be reused, but will not be disposed of at instance destruction.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="context">The device context to use.</param>
        /// <param name="quality">The required render quality.</param>
        /// <param name="profile">The desired optical profile.</param>
        public EyeDiffraction(Device device, DeviceContext context, RenderQuality quality, OpticalProfile profile)
        {
            Pass = new SurfacePass(device);

            Device = device;            /* Store the device. */
            Context = context;          /* Save the context. */
            Quality = quality;          /* Validate quality. */
            Profile = profile;          /* Use lens profile. */
        }

        private void LoadAperture(String path)
        {
            // TODO: for now just load the aperture from a default image (change this later)
            Resource defaultAperture = Texture2D.FromFile(Device, path);
            ShaderResourceView view = new ShaderResourceView(Device, defaultAperture);

            Pass.Pass(Context, @"
            texture2D source                : register(t0);

            SamplerState texSampler
            {
                BorderColor = float4(0, 0, 0, 1);
                Filter = MIN_MAG_MIP_LINEAR;
                AddressU = Border;
                AddressV = Border;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float3 main(PS_IN input) : SV_Target
            {
                return source.Sample(texSampler, input.tex).xyz;
            }
            ", aperture.Dimensions, aperture.RTV, new[] { view }, null);

            view.Dispose();
            defaultAperture.Dispose();
        }

        /// <summary>
        /// Superimposes eye diffraction effects on a texture, created with render target
        /// and shader resource bind flags. The texture should have a high dynamic range.
        /// </summary>
        /// <param name="renderSize">The dimensions of the render target.</param>
        /// <param name="target">The render target to which to render the output.</param>
        /// <param name="source">The source texture to add diffraction effects to.</param>
        /// <param name="dt">The time elapsed since the last call, in seconds.</param>
        public void Render(Size renderSize, RenderTargetView target, ShaderResourceView source, double dt = 0)
        {
            // TODO here generate aperture

            Time += dt;

            diffraction.Diffract(Device, Context, Pass, spectrum.Dimensions, spectrum.RTV, aperture.SRV, Profile.FNumber);

            convolution.Convolve(Device, Context, Pass, renderSize, target, spectrum.SRV, source);
        }

        #region IDisposable

        /// <summary>
        /// Destroys this LensFlare instance.
        /// </summary>
        ~EyeDiffraction()
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

                Pass.Dispose();
            }
        }

        #endregion
    }
}
