using System;
using System.Drawing;

using SharpDX;
using SharpDX.Windows;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

using Insight;

namespace Sample
{
    class Renderer : IDisposable
    {
        private Device device;

        private SwapChain swapChain;

        private GraphicsResource hdrTarget, temporary, output;

        private Texture2D backBuffer;

        private RenderTargetView renderTarget;

        private LensFlare lensFlare;

        public Renderer(RenderForm window)
        {
            Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug, new SwapChainDescription()
            {
                BufferCount = 1,
                IsWindowed = true,
                Flags = SwapChainFlags.None,
                OutputHandle = window.Handle,
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                ModeDescription = new ModeDescription()
                {
                    Format = Format.R8G8B8A8_UNorm,
                    Width = window.ClientSize.Width,
                    Height = window.ClientSize.Height,
                    RefreshRate = new Rational(60, 1),
                    Scaling = DisplayModeScaling.Centered,
                    ScanlineOrdering = DisplayModeScanlineOrder.Progressive
                }
            }, out device, out swapChain);

            hdrTarget = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true, true);
            temporary = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true, true);
            backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            renderTarget = new RenderTargetView(device, backBuffer);

            output = new GraphicsResource(device, window.ClientSize, backBuffer.Description.Format, true, false);

            lensFlare = new LensFlare(device, window.ClientSize, RenderQuality.Medium, new OpticalProfile());
        }

        public void Render()
        {
            // render here into hdrTarget

            lensFlare.Pass.Pass(device, @"
            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float4 main(PS_IN input) : SV_Target
            {
                input.tex = (input.tex - 0.5f) * 2;

                if (pow(input.tex.x, 2) + pow(input.tex.y, 4) < pow(0.05f, 2)) return float4(1, 1, 1, 1);
                else return float4(0, 0, 0, 1);
            }
            ", hdrTarget.RTV, null, null);

            // flares here

            lensFlare.Render(hdrTarget.RTV, hdrTarget.SRV, 1.0 / 60.0);


            // tonemap here

            Tonemap(hdrTarget, output, temporary, 100);

            device.ImmediateContext.CopyResource(output.Resource, backBuffer);

            swapChain.Present(0, PresentFlags.None);
        }

        private void Tonemap(GraphicsResource source, GraphicsResource destination, GraphicsResource temporary, double exposure)
        {
            lensFlare.Pass.Pass(device, @"
            texture2D source             : register(t0);

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float luminance(float3 rgb)
            {
                return dot(rgb, float3(0.2126f, 0.7152f, 0.0722f));
            }

            float4 main(PS_IN input) : SV_Target
            {
                uint w, h, m;

                source.GetDimensions(0, w, h, m);
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);

                float3 rgb = source.Load(int3(x, y, 0)).xyz;

                return float4(rgb, log(luminance(rgb) + 1e-5f));
            }
            ", temporary.RTV, new[] { source.SRV }, null);

            device.ImmediateContext.GenerateMips(temporary.SRV);

            DataStream cbuffer = new DataStream(4, true, true);
            cbuffer.Write<float>((float)exposure);
            cbuffer.Position = 0;

            lensFlare.Pass.Pass(device, @"
            texture2D source             : register(t0);

            cbuffer constants : register(b0)
            {
                float exposure;
            }

            struct PS_IN
            {
	            float4 pos : SV_POSITION;
	            float2 tex :    TEXCOORD;
            };

            float luminance(float3 rgb)
            {
                return dot(rgb, float3(0.2126f, 0.7152f, 0.0722f));
            }

            float4 main(PS_IN input) : SV_Target
            {
                uint w, h, mipLevels;

                source.GetDimensions(0, w, h, mipLevels);
	            uint x = uint(input.tex.x * w);
	            uint y = uint(input.tex.y * h);

                float log_avg = exp(source.Load(int3(0, 0, mipLevels - 1)).w / (w * h));

                float3 rgb = source.Load(int3(x, y, 0)).xyz;
                float  lum = luminance(rgb);

                float key = exposure / log_avg;

                rgb *= key / (1.0f + lum * key);

                return float4(rgb, 1);
            }
            ", destination.RTV, new[] { temporary.SRV }, cbuffer);

            cbuffer.Dispose();
        }
        
        #region IDisposable

        /// <summary>
        /// Destroys this Renderer instance.
        /// </summary>
        ~Renderer()
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
                renderTarget.Dispose();
                backBuffer.Dispose();
                hdrTarget.Dispose();
                temporary.Dispose();
                output.Dispose();
                lensFlare.Dispose();
                swapChain.Dispose();
                device.Dispose();
            }
        }

        #endregion
    }
}
