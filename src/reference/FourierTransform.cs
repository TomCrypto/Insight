using System;
using System.Drawing;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Insight
{
    public struct FFTBuffer
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
    public static class FFTUtils
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
        /// <param name="pass">A SurfacePass instance.</param>
        /// <param name="destination">The destination render target view.</param>
        /// <param name="source">The source texture, can be the same resource as the render target.</param>
        /// <param name="fNumber">The distance at which to evaluate the aperture transmission function.</param>
        public void Diffract(Device device, DeviceContext context, SurfacePass pass, Size renderSize, RenderTargetView destination, ShaderResourceView source, double fNumber)
        {
            if (source.Description.Dimension != ShaderResourceViewDimension.Texture2D)
                throw new ArgumentException("Source SRV must point to a Texture2D resource of suitable dimensions.");

            //if (new Size(source.ResourceAs<Texture2D>().Description.Width, source.ResourceAs<Texture2D>().Description.Height) != resolution)
            //    throw new ArgumentException("Source texture must be the same dimensions as diffraction resolution.");

            pass.Pass(device, context, @"                                                                                       /* 1. Transcode source texture into input FFT buffer. */
            Texture2D<float>    gTex : register(t0);
            RWByteAddressBuffer dest : register(u1);

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float main(PS_IN input) : SV_Target
            {
                uint w, h, m;

                gTex.GetDimensions(0, w, h, m);
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);
                uint index = (y * w + x) << 3U;

                float2 value = float2(gTex.Load(int3(x, y, 0)), 0);
                dest.Store2(index, asuint(value));

	            return 0;
            }
            ", new ViewportF(0, 0, resolution.Width, resolution.Height), null, new[] { source }, new[] { buffer.view }, null);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            UnorderedAccessView fftView = fft.ForwardTransform(buffer.view);

            pass.Pass(device, context, @"                                                                                       /* 2. Transcode output FFT buffer into transform texture. */
            RWByteAddressBuffer buffer : register(u1);

            cbuffer constants : register(b0)
            {
                uint w, h;
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float main(PS_IN input) : SV_Target
            {
	            uint x = (uint(input.tex.x * w) + w / 2) % w;
	            uint y = (uint(input.tex.y * h) + h / 2) % h;
                uint index = (y * w + x) << 3U;

                float2 value = asfloat(buffer.Load2(index));
	            return pow(value.x, 2) + pow(value.y, 2);
            }
            ", new ViewportF(0, 0, transform.Dimensions.Width, transform.Dimensions.Height), transform.RTV, null, new[] { fftView }, cbuffer);

            fftView.Dispose();
            cbuffer.Dispose();

            cbuffer = new DataStream(4, true, true);
            cbuffer.Write<float>((float)fNumber);
            cbuffer.Position = 0;

            pass.Pass(device, context, @"                                                                                       /* 3. Write diffraction spectrum into mipmapped texture. */
            Texture2D<float> transform : register(t0);

            SamplerState texSampler
            {
                BorderColor = float4(0, 0, 0, 1);
                Filter = MIN_MAG_MIP_LINEAR;
                AddressU = Border;
                AddressV = Border;
            };

            cbuffer constants : register(b0)
            {
                float z; // observation plane distance
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            static const uint SPECTRAL_SAMPLES = 80;
            static const float MAX_WAVELENGTH = 775;

            static const float4 colors[SPECTRAL_SAMPLES] =
            {
                float4(0.058, 0.000, 1.000, 380.0),
                float4(0.021, 0.000, 1.000, 385.0),
                float4(0.030, 0.000, 1.000, 390.0),
                float4(0.031, 0.000, 1.000, 395.0),
                float4(0.032, 0.000, 1.000, 400.0),
                float4(0.033, 0.000, 1.000, 405.0),
                float4(0.030, 0.000, 1.000, 410.0),
                float4(0.028, 0.000, 1.000, 415.0),
                float4(0.025, 0.000, 1.000, 420.0),
                float4(0.019, 0.000, 1.000, 425.0),
                float4(0.011, 0.000, 1.000, 430.0),
                float4(0.000, 0.000, 1.000, 435.0),
                float4(0.000, 0.015, 1.000, 440.0),
                float4(0.000, 0.033, 1.000, 445.0),
                float4(0.000, 0.058, 1.000, 450.0),
                float4(0.000, 0.088, 1.000, 455.0),
                float4(0.000, 0.125, 1.000, 460.0),
                float4(0.000, 0.170, 1.000, 465.0),
                float4(0.000, 0.236, 1.000, 470.0),
                float4(0.000, 0.326, 1.000, 475.0),
                float4(0.000, 0.449, 1.000, 480.0),
                float4(0.000, 0.610, 1.000, 485.0),
                float4(0.000, 0.813, 1.000, 490.0),
                float4(0.000, 1.000, 0.947, 495.0),
                float4(0.000, 1.000, 0.755, 500.0),
                float4(0.000, 1.000, 0.621, 505.0),
                float4(0.000, 1.000, 0.520, 510.0),
                float4(0.000, 1.000, 0.440, 515.0),
                float4(0.000, 1.000, 0.375, 520.0),
                float4(0.000, 1.000, 0.319, 525.0),
                float4(0.000, 1.000, 0.258, 530.0),
                float4(0.000, 1.000, 0.191, 535.0),
                float4(0.000, 1.000, 0.109, 540.0),
                float4(0.000, 1.000, 0.000, 545.0),
                float4(0.136, 1.000, 0.000, 550.0),
                float4(0.293, 1.000, 0.000, 555.0),
                float4(0.480, 1.000, 0.000, 560.0),
                float4(0.706, 1.000, 0.000, 565.0),
                float4(0.984, 1.000, 0.000, 570.0),
                float4(1.000, 0.751, 0.000, 575.0),
                float4(1.000, 0.563, 0.000, 580.0),
                float4(1.000, 0.426, 0.000, 585.0),
                float4(1.000, 0.324, 0.000, 590.0),
                float4(1.000, 0.245, 0.000, 595.0),
                float4(1.000, 0.187, 0.000, 600.0),
                float4(1.000, 0.143, 0.000, 605.0),
                float4(1.000, 0.109, 0.000, 610.0),
                float4(1.000, 0.084, 0.000, 615.0),
                float4(1.000, 0.065, 0.000, 620.0),
                float4(1.000, 0.050, 0.000, 625.0),
                float4(1.000, 0.039, 0.000, 630.0),
                float4(1.000, 0.030, 0.000, 635.0),
                float4(1.000, 0.023, 0.000, 640.0),
                float4(1.000, 0.017, 0.000, 645.0),
                float4(1.000, 0.013, 0.000, 650.0),
                float4(1.000, 0.010, 0.000, 655.0),
                float4(1.000, 0.007, 0.000, 660.0),
                float4(1.000, 0.006, 0.000, 665.0),
                float4(1.000, 0.005, 0.000, 670.0),
                float4(1.000, 0.004, 0.000, 675.0),
                float4(1.000, 0.003, 0.000, 680.0),
                float4(1.000, 0.002, 0.000, 685.0),
                float4(1.000, 0.001, 0.000, 690.0),
                float4(1.000, 0.001, 0.000, 695.0),
                float4(1.000, 0.000, 0.000, 700.0),
                float4(1.000, 0.000, 0.001, 705.0),
                float4(1.000, 0.002, 0.000, 710.0),
                float4(1.000, 0.005, 0.000, 715.0),
                float4(1.000, 0.000, 0.011, 720.0),
                float4(1.000, 0.000, 0.007, 725.0),
                float4(1.000, 0.000, 0.002, 730.0),
                float4(1.000, 0.030, 0.000, 735.0),
                float4(1.000, 0.000, 0.049, 740.0),
                float4(1.000, 0.030, 0.000, 745.0),
                float4(1.000, 0.000, 0.018, 750.0),
                float4(1.000, 0.108, 0.000, 755.0),
                float4(1.000, 0.108, 0.000, 760.0),
                float4(1.000, 0.000, 0.185, 765.0),
                float4(1.000, 0.000, 0.185, 770.0),
                float4(1.000, 0.000, 0.185, 775.0),
            };

            float3 main(PS_IN input) : SV_Target
            {
                float3 color = float3(0, 0, 0);

                for (uint t = 0; t < SPECTRAL_SAMPLES; ++t)
                {
                    float scalingFactor = MAX_WAVELENGTH / (z * colors[t].w);
                    float2 coords = (input.tex - 0.5f) * scalingFactor + 0.5f;
                    color += colors[t].xyz * transform.Sample(texSampler, coords); // add scaling factor here??
                }

                return color / SPECTRAL_SAMPLES;
            }
            ", spectrum.Dimensions, spectrum.RTV, new[] { transform.SRV }, cbuffer);

            context.GenerateMips(spectrum.SRV);

            cbuffer.Dispose();
            cbuffer = new DataStream(4, true, true);
            cbuffer.Write<float>((float)fNumber);
            cbuffer.Position = 0;

            pass.Pass(device, context, @"                                                                                       /* 4. Normalize spectrum using lowest mip, and output to destination. */
            Texture2D<float3> spectrum : register(t0);

            cbuffer constants : register(b0)
            {
                float z; // observation plane distance
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            static const float threshold = 1e-10f; // for removing ultra-low-amplitude background noise

            float3 main(PS_IN input) : SV_Target
            {
                uint w, h, maxMips;
                spectrum.GetDimensions(0, w, h, maxMips);

                uint x = uint(input.tex.x * w);
                uint y = uint(input.tex.y * h);

                float3 norm = spectrum.Load(int3(0, 0, maxMips - 1)) * (w * h);
                return max(0, spectrum.Load(int3(x, y, 0)) / norm - threshold) * pow(z, -4);
            }
            ", renderSize, destination, new[] { spectrum.SRV }, cbuffer);

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

        /// <summary>
        /// Creates a ConvolutionEngine instance.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="resolution">The convolution resolution.</param>
        public ConvolutionEngine(Device device, Size resolution)
        {
            fft = FastFourierTransform.Create2DComplex(device.ImmediateContext, resolution.Width, resolution.Height);
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
        }

        public void Convolve(Device device, DeviceContext context, SurfacePass pass, Size renderSize, RenderTargetView destination, ShaderResourceView a, ShaderResourceView b)
        {
            pass.Pass(device, context, @"
            Texture2D<float3> gTex : register(u1);

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
	            return gTex.Sample(texSampler, input.tex);
            }
            ", staging.Dimensions, staging.RTV, new[] { b }, null);

            ConvolveChannel(device, context, pass, a, staging.SRV, rConvolved, "x");
            ConvolveChannel(device, context, pass, a, staging.SRV, gConvolved, "y");
            ConvolveChannel(device, context, pass, a, staging.SRV, bConvolved, "z");

            pass.Pass(device, context, @"                                                                                       /* Finally, compose convolved channels into an RGB image. */
            Texture2D<float> rTex : register(t0);
            Texture2D<float> gTex : register(t1);
            Texture2D<float> bTex : register(t2);
            Texture2D<float3> tex : register(t3);

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
                /*uint w, h, m;

                rTex.GetDimensions(0, w, h, m);
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);

                x = (x + w / 2) / 2;
                y = (y + h / 2) / 2;

                float r = rTex.Load(int3(x, y, 0));
                float g = gTex.Load(int3(x, y, 0));
                float b = bTex.Load(int3(x, y, 0));*/

                float2 offset = (input.tex + 0.5) / 2;

                float r = rTex.Sample(texSampler, offset);
                float g = gTex.Sample(texSampler, offset);
                float b = bTex.Sample(texSampler, offset);

                return float3(r, g, b) + tex.Sample(texSampler, input.tex);
            }
            ", renderSize, destination, new[] { rConvolved.SRV, gConvolved.SRV, bConvolved.SRV, b }, null);
        }

        private void ZeroPad(Device device, DeviceContext context, SurfacePass pass, ShaderResourceView source, UnorderedAccessView target, String channel, bool sub)
        {
            ViewportF viewport = new ViewportF(0, 0, resolution.Width, resolution.Height);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            if (sub)
            {
                pass.Pass(device, context, @"
            Texture2D<float3>   gTex : register(t0);
            RWByteAddressBuffer dest : register(u1);

            cbuffer constants : register(b0)
            {
                uint w, h;
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float main(PS_IN input) : SV_Target
            {
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);
                uint index = (y * w + x) << 3U;

                float intensity = gTex.Load(int3(x, y, 0))." + channel + @";

                dest.Store2(index, asuint(float2(intensity, 0)));

                return 0;
            }
            ", viewport, null, new[] { source }, new[] { target }, cbuffer);
            }
            else
            {
                pass.Pass(device, context, @"
            Texture2D<float3>   gTex : register(t0);
            RWByteAddressBuffer dest : register(u1);

            cbuffer constants : register(b0)
            {
                uint w, h;
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float main(PS_IN input) : SV_Target
            {
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);
                uint index = (y * w + x) << 3U;

                float intensity = gTex.Load(int3(x, y, 0))." + channel + @";
                dest.Store2(index, asuint(float2(intensity, 0)));

                return 0;
            }
            ", viewport, null, new[] { source }, new[] { target }, cbuffer);
            }

            cbuffer.Dispose();
        }

        private void ConvolveChannel(Device device, DeviceContext context, SurfacePass pass, ShaderResourceView sourceA, ShaderResourceView sourceB, GraphicsResource target, String channel)
        {
            if ((channel != "x") && (channel != "y") && (channel != "z")) throw new ArgumentException("Invalid RGB channel specified.");

            ViewportF viewport = new ViewportF(0, 0, resolution.Width, resolution.Height);

            ZeroPad(device, context, pass, sourceA, lBuf.view, channel, true);
            ZeroPad(device, context, pass, sourceB, rBuf.view, channel, false);

            fft.ForwardTransform(lBuf.view, tBuf.view);
            fft.ForwardTransform(rBuf.view, lBuf.view);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            pass.Pass(device, context, @"                                                                                       /* 3. Pointwise multiply FFT(A) and FFT(B). */
            RWByteAddressBuffer bufA : register(u1);
            RWByteAddressBuffer bufB : register(u2);

            cbuffer constants : register(b0)
            {
                uint w, h;
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float2 complex_mul(float2 a, float2 b)
            {
                return float2(a.x * b.x - a.y * b.y,
                              a.y * b.x + a.x * b.y);
            }

            float main(PS_IN input) : SV_Target
            {
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);
                uint index = (y * w + x) << 3U;

                float2 valA = asfloat(bufA.Load2(index));
                float2 valB = asfloat(bufB.Load2(index));

                bufA.Store2(index, asuint(complex_mul(valA, valB)));

                return 0;
            }
            ", viewport, null, null, new[] { tBuf.view, lBuf.view }, cbuffer);

            cbuffer.Dispose();

            cbuffer = new DataStream(8, true, true);
            cbuffer.Write<uint>((uint)resolution.Width);
            cbuffer.Write<uint>((uint)resolution.Height);
            cbuffer.Position = 0;

            UnorderedAccessView fftView = fft.InverseTransform(tBuf.view);

            pass.Pass(device, context, @"                                                                                       /* 4. Transcode IFFT(FFT(A) · FFT(B)) to texture. */
            RWByteAddressBuffer buf : register(u1);

            cbuffer constants : register(b0)
            {
                uint w, h;
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float main(PS_IN input) : SV_Target
            {
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);
                uint index = (y * w + x) << 3U;

                float2 c = asfloat(buf.Load2(index));
                return sqrt(pow(c.x, 2) + pow(c.y, 2));
            }
            ", target.Dimensions, target.RTV, null, new[] { fftView }, cbuffer);

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
