using System;

using SharpDX.Direct3D11;

namespace Insight
{
    /// <summary>
    /// Represents an abstract aperture layer, which adds 
    /// arbitrary features to the aperture's transmission
    /// function, in order to compose various diffraction
    /// effects.
    /// </summary>
    abstract class ApertureLayer : IDisposable
    {
        /// <summary>
        /// Applies the layer to an aperture texture. The output
        /// is to be understood as the transmittance through any
        /// pixel, and will be blended multiplicatively together
        /// with other layers (as such, order does not matter).
        /// </summary>
        /// <param name="context">The device context.</param>
        /// <param name="output">The output aperture.</param>
        /// <param name="profile">An optical profile.</param>
        /// <param name="pass">A SurfacePass instance.</param>
        /// <param name="time">The elapsed time.</param>
        /// <param name="dt">The time since last call.</param>
        public abstract void ApplyLayer(DeviceContext context, GraphicsResource output, OpticalProfile profile, SurfacePass pass, double time, double dt);

        #region IDisposable

        /// <summary>
        /// Destroys this ApertureLayer instance.
        /// </summary>
        ~ApertureLayer()
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

        /// <summary>
        /// Use this method to dispose of unmanaged resources
        /// if and only if disposing is equal to true.
        /// </summary>
        /// <param name="disposing">Whether to dispose.</param>
        protected abstract void Dispose(bool disposing);

        #endregion
    }
}
