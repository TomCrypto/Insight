using System;
using System.Drawing;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D11;

using Insight.Layers;

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
        /// The multiplicative blend state.
        /// </summary>
        private BlendState blendState;

        /// <summary>
        /// A collection of layers to compose the aperture from.
        /// </summary>
        private List<ApertureLayer> layers = new List<ApertureLayer>();

        /// <summary>
        /// Creates a new ApertureComposer instance.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        public ApertureComposer(Device device)
        {
            BlendStateDescription description = new BlendStateDescription()
            {
                 AlphaToCoverageEnable = false,
                 IndependentBlendEnable = false,
            };

            description.RenderTarget[0] = new RenderTargetBlendDescription()
            {
                IsBlendEnabled = true,

                SourceBlend      = BlendOption.Zero,
                DestinationBlend = BlendOption.SourceColor,
                BlendOperation   = BlendOperation.Add,
                
                SourceAlphaBlend      = BlendOption.Zero,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation   = BlendOperation.Add,

                RenderTargetWriteMask = ColorWriteMaskFlags.Red,
            };

            blendState = new BlendState(device, description);

            // Instantiate all layers here

            layers.Add(new StructuralLayer());
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
            context.ClearRenderTargetView(output.RTV, Color4.White);
            context.OutputMerger.SetBlendState(blendState);

            foreach (ApertureLayer layer in layers)
                layer.ApplyLayer(context, output, profile, pass);

            context.OutputMerger.SetBlendState(null);
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
                blendState.Dispose();

                foreach (ApertureLayer layer in layers) layer.Dispose();
            }
        }

        #endregion
    }
}
