using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

using Iridium;

namespace Sample
{
    enum DisplayState
    {
        APERTURE_TRANSMISSION_FUNCTION,
        APERTURE_CONVOLUTION_FILTER,
        ORIGINAL_FRAME,
        CONVOLVED_FRAME,
    }

    class Renderer : IDisposable
    {
        private Device device;
        private SwapChain swapChain;

        private Texture2D backBuffer;
        private RenderTargetView renderTarget;
        private GraphicsResource intermediate;
        private GraphicsResource renderBrightness;

        private Iridium.Iridium iridium;

        public LensFilter LensFilter { get; private set; }

        private DisplayState displayState = DisplayState.APERTURE_TRANSMISSION_FUNCTION;
        public DisplayState DisplayState
        {
            get { return displayState; }
            set { displayState = value; }
        }

        private float renderExposure = (float)Math.Pow(2, 8.5);
        public float Exposure
        {
            get { return renderExposure; }
            set
            {
                renderExposure = value;
                Render();
            }
        }

        private float offset = 0;
        private float speed = 0.25f;
        public float Speed
        {
            get { return speed; }
            set
            {
                offset += (float)stopWatch.ElapsedMilliseconds * speed / 1000.0f;
                stopWatch.Restart();
                speed = value;
            }
        }

        public String AnimationShader { get; set; }

        private Stopwatch stopWatch;

        public Device Device
        {
            get { return device; }
        }

        public Renderer(IntPtr handle, Size dimensions)
        {
            Factory factory = new Factory();
            var adapter = factory.GetAdapter(0);

            device = new Device(adapter, DeviceCreationFlags.Debug);
            //device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.SingleThreaded);
            stopWatch = new Stopwatch();
            stopWatch.Start();

            swapChain = new SwapChain(factory, device, new SwapChainDescription
            {
                BufferCount = 1,
                IsWindowed = true,
                OutputHandle = handle,
                Flags = SwapChainFlags.None,
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                ModeDescription = new ModeDescription(dimensions.Width, dimensions.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            });

            backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderTarget = new RenderTargetView(device, backBuffer);

            intermediate     = new GraphicsResource(device, dimensions,     Format.R32G32B32A32_Float, true, true, true, false);
            renderBrightness = new GraphicsResource(device, new Size(1, 1), Format.R32_Float, true, true);

            LensFilter = new LensFilter(device, new LensFilterDescription()
            {
                apertureSize = new Size(dimensions.Width, dimensions.Height),
                frameSize = new Size(dimensions.Width, dimensions.Height),
            });

            iridium = new Iridium.Iridium(device, new Size(600, 600), RenderQuality.Low, new OpticalProfile());
        }

        /// <summary>
        /// Tonemaps an HDR texture into the backbuffer.
        /// </summary>
        /// <param name="source"></param>
        private void Tonemap(GraphicsResource source)
        {
            /* First of all, do a pass to calculate the log-average brightness for each pixel. */

            DataStream stream = new DataStream(12, true, true);
            stream.Write<int>(LensFilter.Description.apertureSize.Width);
            stream.Write<int>(LensFilter.Description.apertureSize.Height);
            stream.Position = 0;

            LensFilter.Processor.ExecuteReadbackShader(Device, source.RT, null, stream, @"
            texture2D render : register(t0);

            SamplerState texSampler;

            cbuffer stuff
            {
                int width, height;
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

            float luminance(float3 rgb)
            {
                return (0.2126f * rgb.x) + (0.7152f * rgb.y) + (0.0722f * rgb.z);
            }

            float4 main(PS_IN input) : SV_Target
            {
                float3 rgb = render.Sample(texSampler, input.uv.xy).xyz;
                float  l   = log(luminance(rgb) + 1e-5f);
                return float4(rgb, l);
            }
            ");

            stream.Dispose();

            device.ImmediateContext.GenerateMips(source.SRV);

            stream = new DataStream(8, true, true);
            stream.Write<int>(GraphicsUtils.MipLevels(GraphicsUtils.TextureSize(source.Resource)));
            stream.Write<int>(LensFilter.Description.apertureSize.Width * LensFilter.Description.apertureSize.Height);
            stream.Position = 0;

            /* Here we render to a 1x1 texture to fetch the lowest mip level's value, containing the log-average render brightness. */

            LensFilter.Processor.ExecuteShader(Device, renderBrightness.RT, new ShaderResourceView[] { source.SRV }, stream, @"
            texture2D render : register(t1);

            cbuffer LOD
            {
                int level;
                int pixelCount;
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
                float logAvg = render.Load(int3(0, 0, level - 1)).w;
                float lumAvg = exp(logAvg / pixelCount);

                return float4(lumAvg, 0, 0, 1.0f);
            }
            ");

            stream.Dispose();

            /* Finally, we do a final pass into the destination render target, tonemapping as we go. */

            stream = new DataStream(4, true, true);
            stream.Write<float>(renderExposure);
            stream.Position = 0;

            LensFilter.Processor.ExecuteShader(Device, renderTarget, new ShaderResourceView[] { source.SRV, renderBrightness.SRV }, stream, @"
            texture2D render : register(t1);
            texture2D logAvg : register(t2);

            SamplerState texSampler;

            cbuffer stuff
            {
                float exposure;
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

            float luminance(float3 rgb)
            {
                return (0.2126f * rgb.x) + (0.7152f * rgb.y) + (0.0722f * rgb.z);
            }

            float4 main(PS_IN input) : SV_Target
            {
                float3 rgb = render.Sample(texSampler, input.uv.xy).xyz;
                float  lum = luminance(rgb);

                float key = exposure / logAvg.Load(int3(0, 0, 0)).x;

                rgb *= key / (1.0f + lum * key);

                return float4(rgb, 1.0f);
            }
            ");

            stream.Dispose();
        }

        private void SynthesizeFrame(RenderTargetView renderTarget)
        {
            float elapsed = (float)stopWatch.ElapsedMilliseconds / 1000.0f;

            DataStream stream = new DataStream(4, true, true);
            stream.Write<float>(elapsed * Speed + offset);
            stream.Position = 0;

            LensFilter.Processor.ExecuteShader(Device, renderTarget, null, stream, AnimationShader);

            stream.Dispose();
        }

        public void setScale(float scale)
        {
            LensFilter.Scale = scale;
        }

        private SpectralTerm[] spectralTerms = 
        {
            new SpectralTerm()
            {
                wavelength = 450,
                rgbFilter = new Color4(0, 0, 1, 1)
            },
            new SpectralTerm()
            {
                wavelength = 525,
                rgbFilter = new Color4(0, 1, 0, 1)
            },
            new SpectralTerm()
            {
                wavelength = 650,
                rgbFilter = new Color4(1, 0, 0, 1)
            }
        };

        public void SetAperture(Texture2D texture)
        {
            ShaderResourceView SRV = new ShaderResourceView(device, texture);
            LensFilter.SetAperture(SRV);
            SRV.Dispose();
        }

        public void Refresh()
        {
            LensFilter.GenerateConvolutionFilter();
        }

        public void Render()
        {
            switch (displayState)
            {
                case DisplayState.APERTURE_TRANSMISSION_FUNCTION:
                    LensFilter.RenderAperture(renderTarget);
                    break;

                case DisplayState.APERTURE_CONVOLUTION_FILTER:
                    LensFilter.RenderFilter(intermediate.RT);
                    Tonemap(intermediate);
                    break;

                case DisplayState.ORIGINAL_FRAME:
                    LensFilter.RenderAperture(intermediate.RT);
                    iridium.Augment(intermediate.Resource, 1.0/60.0);
                    Tonemap(intermediate);
                    break;

                case DisplayState.CONVOLVED_FRAME:
                    SynthesizeFrame(intermediate.RT);
                    LensFilter.Convolve(intermediate.RT, intermediate.SRV);
                    Tonemap(intermediate);
                    break;
            }

            swapChain.Present(1, PresentFlags.None);
        }

        #region IDisposable

        ~Renderer()
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
                LensFilter.Dispose();
                iridium.Dispose();

                renderBrightness.Dispose();
                intermediate.Dispose();
                renderTarget.Dispose();
                backBuffer.Dispose();

                swapChain.Dispose();
                device.Dispose();
            }
        }

        #endregion
    }
}
