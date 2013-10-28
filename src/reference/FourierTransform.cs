using System;
using System.Text;
using System.Drawing;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Insight
{
    struct FFTBuffer
    {
        public UnorderedAccessView view;
        public Buffer buffer;

        public FFTBuffer(Buffer buffer, UnorderedAccessView view)
        {
            this.buffer = buffer;
            this.view = view;
        }
    }

    /// <summary>
    /// Provides utility methods for the FFT classes.
    /// </summary>
    static class FFTUtils
    {
        /// <summary>
        /// Allocates a raw buffer UAV with a given float count.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="floatCount">The number of floats in the buffer.</param>
        /// <returns>The allocated raw buffer.</returns>
        public static FFTBuffer AllocateBuffer(Device device, int floatCount)
        {
            Buffer buffer = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                SizeInBytes = floatCount * sizeof(float),
                StructureByteStride = sizeof(float),
                Usage = ResourceUsage.Default
            });

            UnorderedAccessView view = new UnorderedAccessView(device, buffer, new UnorderedAccessViewDescription()
            {
                Format = SharpDX.DXGI.Format.R32_Typeless,
                Dimension = UnorderedAccessViewDimension.Buffer,

                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = floatCount,
                    Flags = UnorderedAccessViewBufferFlags.Raw,
                }
            });

            return new FFTBuffer(buffer, view);
        }
    }

    /// <summary>
    /// This class assists in computing diffraction spectrums.
    /// </summary>
    public class DiffractionEngine : IDisposable
    {
        private GraphicsResource transform, spectrum;
        private FFTBuffer[] temporaries;
        private FFTBuffer[] precomputed;
        private FFTBuffer buffer;
        private FastFourierTransform fft;
        private Size resolution;

        /// <summary>
        /// Creates a DiffractionEngine instance.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="context">The graphics context to use.</param>
        /// <param name="resolution">The diffraction resolution.</param>
        public DiffractionEngine(Device device, DeviceContext context, Size resolution)
        {
            fft = FastFourierTransform.Create2DComplex(context, resolution.Width, resolution.Height);
            fft.ForwardScale = 1.0f / (float)(resolution.Width * resolution.Height);
            this.resolution = resolution;

            FastFourierTransformBufferRequirements bufferReqs = fft.BufferRequirements;
            precomputed = new FFTBuffer[bufferReqs.PrecomputeBufferCount];
            temporaries = new FFTBuffer[bufferReqs.TemporaryBufferCount];

            for (int t = 0; t < precomputed.Length; ++t)
                precomputed[t] = FFTUtils.AllocateBuffer(device, bufferReqs.PrecomputeBufferSizes[t]);

            for (int t = 0; t < temporaries.Length; ++t)
                temporaries[t] = FFTUtils.AllocateBuffer(device, bufferReqs.TemporaryBufferSizes[t]);

            UnorderedAccessView[] precomputedUAV = new UnorderedAccessView[bufferReqs.PrecomputeBufferCount];
            for (int t = 0; t < precomputed.Length; ++t) precomputedUAV[t] = precomputed[t].view;

            UnorderedAccessView[] temporariesUAV = new UnorderedAccessView[bufferReqs.TemporaryBufferCount];
            for (int t = 0; t < temporaries.Length; ++t) temporariesUAV[t] = temporaries[t].view;

            fft.AttachBuffersAndPrecompute(temporariesUAV, precomputedUAV);

            /* We are doing a complex to complex transform, so we need two floats per pixel. */
            buffer = FFTUtils.AllocateBuffer(device, 2 * resolution.Width * resolution.Height);

            transform = new GraphicsResource(device, resolution, Format.R32_Float, true, true);
            spectrum  = new GraphicsResource(device, resolution, Format.R32G32B32A32_Float, true, true, true);
        }

        /// <summary>
        /// Generates the diffraction spectrum of a texture. The source texture
        /// must be the exact resolution specified in the constructor, however,
        /// the output will be resized to the destination texture as needed.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="context">The device context to use.</param>
        /// <param name="pass">A SurfacePass instance.</param>
        /// <param name="renderSize">The dimensions of the render target.</param>
        /// <param name="destination">The destination render target view.</param>
        /// <param name="source">The source texture, can be the same resource as the render target.</param>
        /// <param name="fNumber">The distance at which to evaluate the aperture transmission function.</param>
        public void Diffract(Device device, DeviceContext context, SurfacePass pass, Size renderSize, RenderTargetView destination, ShaderResourceView source, double fNumber)
        {
            if (source.Description.Dimension != ShaderResourceViewDimension.Texture2D)
                throw new ArgumentException("Source SRV must point to a Texture2D resource of suitable dimensions.");

            //if (new Size(source.ResourceAs<Texture2D>().Description.Width, source.ResourceAs<Texture2D>().Description.Height) != resolution)
            //    throw new ArgumentException("Source texture must be the same dimensions as diffraction resolution.");

            pass.Pass(context, Encoding.ASCII.GetString(Resources.DiffractionTexToBuf), new ViewportF(0, 0, resolution.Width, resolution.Height), null, new[] { source }, new[] { buffer.view }, null);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            UnorderedAccessView fftView = fft.ForwardTransform(buffer.view);

            pass.Pass(context, Encoding.ASCII.GetString(Resources.DiffractionBufToTex), new ViewportF(0, 0, transform.Dimensions.Width, transform.Dimensions.Height), transform.RTV, null, new[] { fftView }, cbuffer);

            fftView.Dispose();
            cbuffer.Dispose();

            cbuffer = new DataStream(4, true, true);
            cbuffer.Write<float>((float)fNumber);
            cbuffer.Position = 0;

            pass.Pass(context, Encoding.ASCII.GetString(Resources.DiffractionSpectrum), spectrum.Dimensions, spectrum.RTV, new[] { transform.SRV }, cbuffer);

            context.GenerateMips(spectrum.SRV);

            cbuffer.Dispose();
            cbuffer = new DataStream(4, true, true);
            cbuffer.Write<float>((float)fNumber);
            cbuffer.Position = 0;

            pass.Pass(context, Encoding.ASCII.GetString(Resources.DiffractionNormalize), renderSize, destination, new[] { spectrum.SRV }, cbuffer);

            cbuffer.Dispose();
        }

        #region IDisposable

        /// <summary>
        /// Destroys this DiffractionEngine instance.
        /// </summary>
        ~DiffractionEngine()
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
                foreach (FFTBuffer buf in precomputed)
                {
                    buf.view.Dispose();
                    buf.buffer.Dispose();
                }

                foreach (FFTBuffer buf in temporaries)
                {
                    buf.view.Dispose();
                    buf.buffer.Dispose();
                }

                buffer.view.Dispose();
                buffer.buffer.Dispose();

                transform.Dispose();
                spectrum.Dispose();

                fft.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// This class assists in convolving two textures.
    /// </summary>
    public class ConvolutionEngine : IDisposable
    {
        private FFTBuffer lBuf, rBuf, tBuf;
        private FFTBuffer[] temporaries;
        private FFTBuffer[] precomputed;
        private GraphicsResource rConvolved;
        private GraphicsResource gConvolved;
        private GraphicsResource bConvolved;
        private GraphicsResource staging;
        private FastFourierTransform fft;
        private Size resolution;

        private BlendState blendState;

        /// <summary>
        /// Creates a ConvolutionEngine instance.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="context">The graphics context to use.</param>
        /// <param name="resolution">The convolution resolution.</param>
        public ConvolutionEngine(Device device, DeviceContext context, Size resolution)
        {
            fft = FastFourierTransform.Create2DComplex(context, resolution.Width, resolution.Height);
            fft.InverseScale = 1.0f / (float)(resolution.Width * resolution.Height);
            this.resolution = resolution;

            FastFourierTransformBufferRequirements bufferReqs = fft.BufferRequirements;
            precomputed = new FFTBuffer[bufferReqs.PrecomputeBufferCount];
            temporaries = new FFTBuffer[bufferReqs.TemporaryBufferCount];

            for (int t = 0; t < precomputed.Length; ++t)
                precomputed[t] = FFTUtils.AllocateBuffer(device, bufferReqs.PrecomputeBufferSizes[t]);

            for (int t = 0; t < temporaries.Length; ++t)
                temporaries[t] = FFTUtils.AllocateBuffer(device, bufferReqs.TemporaryBufferSizes[t]);

            UnorderedAccessView[] precomputedUAV = new UnorderedAccessView[bufferReqs.PrecomputeBufferCount];
            for (int t = 0; t < precomputed.Length; ++t) precomputedUAV[t] = precomputed[t].view;

            UnorderedAccessView[] temporariesUAV = new UnorderedAccessView[bufferReqs.TemporaryBufferCount];
            for (int t = 0; t < temporaries.Length; ++t) temporariesUAV[t] = temporaries[t].view;

            fft.AttachBuffersAndPrecompute(temporariesUAV, precomputedUAV);

            lBuf = FFTUtils.AllocateBuffer(device, 2 * resolution.Width * resolution.Height);
            rBuf = FFTUtils.AllocateBuffer(device, 2 * resolution.Width * resolution.Height);
            tBuf = FFTUtils.AllocateBuffer(device, 2 * resolution.Width * resolution.Height);

            rConvolved = new GraphicsResource(device, resolution, Format.R32_Float, true, true);
            gConvolved = new GraphicsResource(device, resolution, Format.R32_Float, true, true);
            bConvolved = new GraphicsResource(device, resolution, Format.R32_Float, true, true);
            staging    = new GraphicsResource(device, new Size(resolution.Width / 2, resolution.Height / 2), Format.R32G32B32A32_Float, true, true);

            BlendStateDescription description = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false,
            };

            description.RenderTarget[0] = new RenderTargetBlendDescription()
            {
                IsBlendEnabled = true,

                SourceBlend = BlendOption.One,
                DestinationBlend = BlendOption.One,
                BlendOperation = BlendOperation.Add,

                SourceAlphaBlend = BlendOption.Zero,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,

                RenderTargetWriteMask = ColorWriteMaskFlags.Red
                                      | ColorWriteMaskFlags.Green
                                      | ColorWriteMaskFlags.Blue,
            };

            blendState = new BlendState(device, description);
        }

        public void Convolve(Device device, DeviceContext context, SurfacePass pass, Size renderSize, RenderTargetView destination, ShaderResourceView a, ShaderResourceView b, bool scaleCorrect)
        {
            pass.Pass(context, Encoding.ASCII.GetString(Resources.ConvolutionRescale), staging.Dimensions, staging.RTV, new[] { b }, null);

            ConvolveChannel(device, context, pass, a, staging.SRV, rConvolved, "x");
            ConvolveChannel(device, context, pass, a, staging.SRV, gConvolved, "y");
            ConvolveChannel(device, context, pass, a, staging.SRV, bConvolved, "z");

            if (scaleCorrect) context.OutputMerger.SetBlendState(blendState);

            pass.Pass(context, Encoding.ASCII.GetString(Resources.ConvolutionCompose), renderSize, destination, new[] { rConvolved.SRV, gConvolved.SRV, bConvolved.SRV }, null); // TODO: better resizing later

            context.OutputMerger.SetBlendState(null);
        }

        private void ZeroPad(Device device, DeviceContext context, SurfacePass pass, ShaderResourceView source, UnorderedAccessView target, String channel)
        {
            ViewportF viewport = new ViewportF(0, 0, resolution.Width, resolution.Height);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            pass.Pass(context, "#define CHANNEL " + channel + "\n" + Encoding.ASCII.GetString(Resources.ConvolutionZeroPad), viewport, null, new[] { source }, new[] { target }, cbuffer);

            cbuffer.Dispose();
        }

        private void ConvolveChannel(Device device, DeviceContext context, SurfacePass pass, ShaderResourceView sourceA, ShaderResourceView sourceB, GraphicsResource target, String channel)
        {
            if ((channel != "x") && (channel != "y") && (channel != "z")) throw new ArgumentException("Invalid RGB channel specified.");

            ViewportF viewport = new ViewportF(0, 0, resolution.Width, resolution.Height);

            ZeroPad(device, context, pass, sourceA, lBuf.view, channel);
            ZeroPad(device, context, pass, sourceB, rBuf.view, channel);

            fft.ForwardTransform(lBuf.view, tBuf.view);
            fft.ForwardTransform(rBuf.view, lBuf.view);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            pass.Pass(context, Encoding.ASCII.GetString(Resources.ConvolutionMultiply), viewport, null, null, new[] { tBuf.view, lBuf.view }, cbuffer);

            cbuffer.Dispose();

            cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            UnorderedAccessView fftView = fft.InverseTransform(tBuf.view);

            pass.Pass(context, Encoding.ASCII.GetString(Resources.ConvolutionOutput), target.Dimensions, target.RTV, null, new[] { fftView }, cbuffer);

            fftView.Dispose();
            cbuffer.Dispose();
        }

        #region IDisposable

        /// <summary>
        /// Destroys this ConvolutionEngine instance.
        /// </summary>
        ~ConvolutionEngine()
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
                foreach (FFTBuffer buf in precomputed)
                {
                    buf.view.Dispose();
                    buf.buffer.Dispose();
                }

                foreach (FFTBuffer buf in temporaries)
                {
                    buf.view.Dispose();
                    buf.buffer.Dispose();
                }

                blendState.Dispose();

                rConvolved.Dispose();
                gConvolved.Dispose();
                bConvolved.Dispose();
                staging.Dispose();
                lBuf.view.Dispose();
                rBuf.view.Dispose();
                tBuf.view.Dispose();
                lBuf.buffer.Dispose();
                rBuf.buffer.Dispose();
                tBuf.buffer.Dispose();
                fft.Dispose();
            }
        }

        #endregion
    }
}
