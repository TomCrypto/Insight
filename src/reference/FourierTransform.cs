using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;

namespace Iridium
{
    public sealed class FourierTransform : IDisposable
    {
        private readonly UnorderedAccessView[] temporaries, precomputed;
        private readonly UnorderedAccessView input, output;
        private readonly ShaderResourceView stagingTexture;
        private readonly FastFourierTransform fft;
        private readonly Buffer dimensionsBuffer;
        private readonly Size dimensions;
        private readonly Device device;

        private GraphicsResource rFrame, gFrame, bFrame;
        private GraphicsResource rFilter, gFilter, bFilter;
        private UnorderedAccessView input2, output2;
        private GraphicsResource tmp;
        private Boolean willConvolve;

        private DataStream PrepareDimensionsBuffer(Size textureDimensions)
        {
            DataStream stream = new DataStream(8, true, true);
            stream.Write<uint>((uint)textureDimensions.Width);
            stream.Write<uint>((uint)textureDimensions.Height);
            stream.Position = 0;
            return stream;
        }

        private void TextureToRawBuffer(ShaderProcessor processor, ShaderResourceView texture, UnorderedAccessView buffer, RenderTargetView renderTarget)
        {
            DataStream stream = PrepareDimensionsBuffer(GraphicsUtils.TextureSize(texture.Resource));

            processor.ExecuteShader(device, renderTarget, buffer, new ShaderResourceView[] { texture }, stream, @"
            RWByteAddressBuffer buffer : register(u1);

            texture2D inTex : register(t1);

            SamplerState texSampler;

            cbuffer dims
            {
	            uint width;
	            uint height;
                float scale;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                uint w, h, m;

                inTex.GetDimensions(0, w, h, m);

                uint x = int(input.uv.x * w);
                uint y = int(input.uv.y * h);

	            /*uint x = int(input.uv.x * width);
	            uint y = int(input.uv.y * height);*/

	            uint2 val = uint2(asuint(inTex.Sample(texSampler, input.uv.xy).x), 0);

	            buffer.Store2(2 * 4 * (y * width + x), val);

	            return float4(1, 1, 1, 1); // dummy output, render target is not used
            }
            ");

            stream.Dispose();
        }

        private void RawBufferToTexturePS(ShaderProcessor processor, UnorderedAccessView buffer, RenderTargetView renderTarget)
        {
            DataStream stream = PrepareDimensionsBuffer(GraphicsUtils.TextureSize(renderTarget.Resource));

            processor.ExecuteShader(device, renderTarget, buffer, null, stream, @"
            RWByteAddressBuffer buffer : register(u1);

            cbuffer dims
            {
	            uint width;
	            uint height;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
	            uint x = int(input.uv.x * width);
	            uint y = int(input.uv.y * height);

	            uint2 val = buffer.Load2(2 * 4 * (y * width + x));

	            float2 complex = float2(asfloat(val.x), asfloat(val.y));

	            return float4(complex.x * complex.x + complex.y * complex.y, 0, 0, 1);
            }
            ");

            stream.Dispose();
        }

        private void RawBufferToTexture(ShaderProcessor processor, UnorderedAccessView buffer, RenderTargetView renderTarget)
        {
            DataStream stream = PrepareDimensionsBuffer(GraphicsUtils.TextureSize(renderTarget.Resource));

            processor.ExecuteShader(device, renderTarget, buffer, null, stream, @"
            RWByteAddressBuffer buffer : register(u1);

            cbuffer dims
            {
	            uint width;
	            uint height;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
	            uint x = int(input.uv.x * width);
	            uint y = int(input.uv.y * height);

	            uint2 val = buffer.Load2(2 * 4 * (y * width + x));

	            float2 complex = float2(asfloat(val.x), asfloat(val.y));

                return float4(sqrt(complex.x * complex.x + complex.y * complex.y), 0, 0, 1); // take magnitude of result
            }
            ");

            stream.Dispose();
        }

        private UnorderedAccessView AllocateBuffer(int floatCount)
        {
            Buffer buffer = new Buffer(device, new BufferDescription()
            {
                BindFlags = SharpDX.Direct3D11.BindFlags.UnorderedAccess | SharpDX.Direct3D11.BindFlags.ShaderResource,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                SizeInBytes = floatCount * sizeof(float),
                StructureByteStride = sizeof(float),
                Usage = ResourceUsage.Default
            });

            return new UnorderedAccessView(device, buffer, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = SharpDX.DXGI.Format.R32_Typeless,

                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = floatCount,
                    //Format = SharpDX.DXGI.Format.R32_Typeless,
                    Flags = UnorderedAccessViewBufferFlags.Raw,
                    //Dimension = UnorderedAccessViewDimension.Buffer,
                }
            });
        }

        public FourierTransform(Device device, Size dimensions, Boolean willConvolve)
        {
            this.willConvolve = willConvolve;
            this.dimensions = dimensions;
            this.device = device;

            fft = FastFourierTransform.Create2DComplex(device.ImmediateContext, dimensions.Width, dimensions.Height);
            FastFourierTransformBufferRequirements bufferReqs = fft.BufferRequirements;

            precomputed = new UnorderedAccessView[bufferReqs.PrecomputeBufferCount];
            temporaries = new UnorderedAccessView[bufferReqs.TemporaryBufferCount];

            for (int t = 0; t < precomputed.Length; ++t)
                precomputed[t] = AllocateBuffer(bufferReqs.PrecomputeBufferSizes[t]);

            for (int t = 0; t < temporaries.Length; ++t)
                temporaries[t] = AllocateBuffer(bufferReqs.TemporaryBufferSizes[t]);

            fft.AttachBuffersAndPrecompute(temporaries, precomputed);

            /* We are doing a complex FFT, so we need 2 floats per pixel. */
            input = AllocateBuffer(2 * dimensions.Width * dimensions.Height);
            output = AllocateBuffer(2 * dimensions.Width * dimensions.Height);

            stagingTexture = new ShaderResourceView(device, new Texture2D(device, new Texture2DDescription()
            {
                ArraySize = 1,
                MipLevels = 1,
                Width = dimensions.Width,
                Height = dimensions.Height,
                Format = Format.R32_Float,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
            }));

            dimensionsBuffer = new Buffer(device, new BufferDescription()
            {
                SizeInBytes = 16,
                StructureByteStride = 16,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
            });

            if (willConvolve) // we'll need all this stuff for a convolution
            {
                rFrame = new GraphicsResource(device, dimensions, Format.R32_Float);
                gFrame = new GraphicsResource(device, dimensions, Format.R32_Float);
                bFrame = new GraphicsResource(device, dimensions, Format.R32_Float);

                rFilter = new GraphicsResource(device, dimensions, Format.R32_Float);
                gFilter = new GraphicsResource(device, dimensions, Format.R32_Float);
                bFilter = new GraphicsResource(device, dimensions, Format.R32_Float);

                tmp = new GraphicsResource(device, dimensions, Format.R32_Float);

                input2 = AllocateBuffer(2 * dimensions.Width * dimensions.Height);
                output2 = AllocateBuffer(2 * dimensions.Width * dimensions.Height);
            }
        }

        /// <summary>
        /// Computes the power spectrum of a two-dimensional texture. Only the R channel
        /// of the texture will be considered, all others will be ignored. The resulting
        /// power spectrum is guaranteed to be normalized, in the sense that summing the
        /// resulting pixel intensities over the entire texture shall produce 1.
        /// </summary>
        /// <remarks>
        /// The source and target textures may not represent the same resource, but they
        /// need not be the same format (though using different formats may lead to loss
        /// of floating-point accuracy).
        /// 
        /// The source texture must be in R32_Float format.
        /// 
        /// 
        /// 
        /// If the source texture's dimensions are smaller than this FourierTransform instance's effective
        /// size, the source texture will be padded with black on the right and bottom. If the dimensions
        /// are larger, an exception will be thrown.
        /// 
        /// If the render target's dimensions are not exactly the size of the instance's effective size, an error will be thrown.
        /// 
        /// Both the input and output are to be viewed as 2D arrays of reals.
        /// </remarks>
        /// <param name="renderTarget">The texture in which to render the power spectrum.</param>
        /// <param name="source">The source texture from which to read the input.</param>
        public void GeneratePowerSpectrum(ShaderProcessor processor, RenderTargetView renderTarget, ShaderResourceView source)
        {
            if (!GraphicsUtils.TextureSize(renderTarget.Resource).Equals(dimensions))
                throw new ArgumentException("Render target must have the same dimensions as this FourierTransform instance's dimensions.");

            if ((GraphicsUtils.TextureSize(source.Resource).Width > dimensions.Width) || (GraphicsUtils.TextureSize(source.Resource).Height > dimensions.Height))
                throw new ArgumentException("Source texture's dimensions may not exceed dimensions of this FourierTransform instance.");

            if (!GraphicsUtils.TextureFormat(renderTarget.Resource).Equals(Format.R32_Float))
                throw new ArgumentException("Render target must have format R32_Float.");

            if (!GraphicsUtils.TextureFormat(source.Resource).Equals(Format.R32_Float))
                throw new ArgumentException("Source texture must have format R32_Float.");

            ResourceRegion fillRegion = new ResourceRegion(0, 0, 0, GraphicsUtils.TextureSize(source.Resource).Width, GraphicsUtils.TextureSize(source.Resource).Height, 1);
            device.ImmediateContext.CopySubresourceRegion(source.Resource, 0, fillRegion, stagingTexture.Resource, 0, 0, 0, 0);

            TextureToRawBuffer(processor, stagingTexture, input, renderTarget);

            fft.ForwardScale = 1.0f/ (dimensions.Width * dimensions.Height);
            fft.ForwardTransform(input, output);

            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);

            RawBufferToTexturePS(processor, output, renderTarget);
        }

        private void ConvolveChannel(ShaderProcessor processor, GraphicsResource frame, GraphicsResource filter, RenderTargetView dummyRT)
        {
            // FFT(A)
            TextureToRawBuffer(processor, frame.SRV, input, dummyRT);
            fft.ForwardTransform(input, output);

            // FFT(B)
            TextureToRawBuffer(processor, filter.SRV, input2, dummyRT);
            fft.ForwardTransform(input2, output2);

            // FFT(A) * FFT(B)

            DataStream stream = PrepareDimensionsBuffer(GraphicsUtils.TextureSize(dummyRT.Resource));

            processor.ExecuteShader(device, dummyRT, new UnorderedAccessView[] { output, output2 }, null, stream, @"
            RWByteAddressBuffer frame : register(u1);
            RWByteAddressBuffer filter : register(u2);

            cbuffer dims
            {
	            uint width;
	            uint height;
            }

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            /* Returns the product a × b */
            float2 cpx_mul(float2 a, float2 b)
            {
	            float2 r;

	            r.x = a.x * b.x - a.y * b.y;
	            r.y = a.y * b.x + a.x * b.y;

	            return r;
            }

            float4 main(PS_IN input) : SV_Target
            {
	            uint x = int(input.uv.x * width);
	            uint y = int(input.uv.y * height);

	            float2 frameCpx  = asfloat(frame.Load2(2 * 4 * (y * width + x)));
                float2 filterCpx = asfloat(filter.Load2(2 * 4 * (y * width + x)));

                float2 output = cpx_mul(frameCpx, filterCpx);

                frame.Store2(2 * 4 * (y * width + x), asuint(output));

	            return float4(1, 1, 1, 1); // dummy output
            }
            ");

            stream.Dispose();

            // IFFT(FFT(A) * FFT(B))
            fft.InverseTransform(output, input);

            RawBufferToTexture(processor, input, frame.RT);
        }

        public void Convolve(ShaderProcessor processor, RenderTargetView renderTarget, ShaderResourceView frame, ShaderResourceView filter, double scale)
        {
            // first decompose the filter into the right channels

            CopyChannel(processor, rFilter.RT, filter, "x");
            CopyChannel(processor, gFilter.RT, filter, "y");
            CopyChannel(processor, bFilter.RT, filter, "z");

            // same for the frame

            CopyChannel(processor, rFrame.RT, frame, "x");
            CopyChannel(processor, gFrame.RT, frame, "y");
            CopyChannel(processor, bFrame.RT, frame, "z");

            // convolve each channel
            ConvolveChannel(processor, rFrame, rFilter, tmp.RT);
            ConvolveChannel(processor, gFrame, gFilter, tmp.RT);
            ConvolveChannel(processor, bFrame, bFilter, tmp.RT);

            // Compose each channel of the frame textures back into an RGB image

            processor.ExecuteShader(device, renderTarget, new ShaderResourceView[] { rFrame.SRV, gFrame.SRV, bFrame.SRV }, null, @"
            texture2D rTex : register(t1);
            texture2D gTex : register(t2);
            texture2D bTex : register(t3);

            SamplerState texSampler;

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                float u = input.uv.x * 2 - 1;
                float v = input.uv.y * 2 - 1;

                u /= 2;
                v /= 2;

                u = (u + 1) * 0.5f;
                v = (v + 1) * 0.5f;

                float2 uv = float2(u, v);

                float r = rTex.Sample(texSampler, uv).x;
                float g = gTex.Sample(texSampler, uv).x;
                float b = bTex.Sample(texSampler, uv).x;

                return float4(r, g, b, 1.0f);
            }
            ");
        }

        public void CopyChannel(ShaderProcessor processor, RenderTargetView target, ShaderResourceView source, String channel)
        {
            processor.ExecuteShader(device, target, new ShaderResourceView[] { source }, null, @"
            texture2D source : register(t1);

            SamplerState texSampler;

            struct VS_IN
            {
	            float4 pos : POSITION;
	            float4 uv  : TEXCOORD;
            };

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float4 uv  : TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
	            return float4(source.Sample(texSampler, input.uv.xy * 2)." + channel + @", 0, 0, 1.0f);
            }
            ");
        }

        #region IDisposable

        ~FourierTransform()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int t = 0; t < precomputed.Length; ++t)
                {
                    precomputed[t].Resource.Dispose();
                    precomputed[t].Dispose();
                }

                for (int t = 0; t < temporaries.Length; ++t)
                {
                    temporaries[t].Resource.Dispose();
                    temporaries[t].Dispose();
                }

                stagingTexture.Resource.Dispose();
                stagingTexture.Dispose();

                dimensionsBuffer.Dispose();
                output.Resource.Dispose();
                input.Resource.Dispose();
                output.Dispose();
                input.Dispose();

                if (willConvolve)
                {
                    rFilter.Dispose();
                    gFilter.Dispose();
                    bFilter.Dispose();

                    rFrame.Dispose();
                    gFrame.Dispose();
                    bFrame.Dispose();

                    input2.Resource.Dispose();
                    output2.Resource.Dispose();
                    input2.Dispose();
                    output2.Dispose();
                    tmp.Dispose();
                }

                fft.Dispose();
            }
        }

        #endregion
    }
}
