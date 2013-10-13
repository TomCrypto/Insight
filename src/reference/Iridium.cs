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
        /// Low quality: aperture rendered at 256×256 resolution, maximum convolution dimensions 512×512 pixels. Extremely fast, for low-end computers.
        /// </summary>
        Low,
        /// <summary>
        /// Medium quality: aperture rendered at 512×512 resolution, maximum convolution dimensions 1024×1024 pixels. Good performance/accuracy balance.
        /// </summary>
        Medium,
        /// <summary>
        /// High quality: aperture rendered at 1024×1024 resolution, maximum convolution dimensions 2048×2048 pixels. For high-end graphics cards only.
        /// </summary>
        High,
        /// <summary>
        /// Optimal quality: aperture rendered at 2048×2048 resolution, maximum convolution dimensions 4096×4096 pixels. Intended for use in offline rendering.
        /// </summary>
        Optimal,
    }

    /// <summary>
    /// Describes how the eye lens aperture should look, and directly influences
    /// the appearance of diffraction effects.
    /// </summary>
    public struct LensProfile
    {
        // put stuff here
    }

    /// <summary>
    /// Provides configurable eye diffraction effects.
    /// </summary>
    public sealed class Iridium
    {
        private RenderQuality quality;
        private LensProfile profile;
        private Size dimensions;
        private double time;

        /// <summary>
        /// The render quality currently used for rendering diffraction effects.
        /// </summary>
        public RenderQuality Quality
        {
            get { return quality; }
            set
            {
                quality = value;
                // do something
            }
        }

        /// <summary>
        /// The lens profile currently used for rendering diffraction effects.
        /// </summary>
        public LensProfile Profile
        {
            get { return profile; }
            set
            {
                profile = value;
                // do something
            }
        }

        /// <summary>
        /// The dimensions of the surface to which to add diffraction effects to.
        /// </summary>
        public Size Dimensions
        {
            get { return dimensions; }
            set
            {
                if (value == null) throw new ArgumentNullException("The surface dimensions cannot be null.");

                dimensions = value;
                // do something
            }
        }

        /// <summary>
        /// Initializes an Iridium instance with custom settings.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="dimensions">The target surface dimensions.</param>
        /// <param name="quality">The required render quality.</param>
        /// <param name="profile">The desired lens profile.</param>
        public Iridium(Device device, Size dimensions, RenderQuality quality, LensProfile profile)
        {
            Dimensions = dimensions;    /* Check dimensions. */
            Quality = quality;          /* Validate quality. */
            Profile = profile;          /* Use lens profile. */
        }

        /// <summary>
        /// Superimposes eye diffraction effects on a texture, created with render target
        /// and shader resource bind flags. The texture should have a high dynamic range.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="surface">The source and destination texture.</param>
        /// <param name="dt">The time elapsed since the last call, in seconds.</param>
        public void Augment(Device device, Texture2D surface, double dt = 0)
        {
            time += dt;
        }
    }
}
