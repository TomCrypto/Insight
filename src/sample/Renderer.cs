using System;
using System.Drawing;
using System.Diagnostics;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Windows;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using DriverType = SharpDX.Direct3D.DriverType;

using Insight;

namespace Sample
{
    /// <summary>
    /// The sample renderer pipeline.
    /// </summary>
    class Renderer : IDisposable
    {
        /// <summary>
        /// Graphics device currently in use.
        /// </summary>
        private Device device;

        /// <summary>
        /// Main swapchain for the rendering.
        /// </summary>
        private SwapChain swapChain;

        /// <summary>
        /// A temporary texture buffer, for tonemapping.
        /// </summary>
        private GraphicsResource temporary;

        /// <summary>
        /// High dynamic range buffer to render the scene.
        /// </summary>
        private GraphicsResource hdrBuffer;

        /// <summary>
        /// Low-depth buffer for the swapchain.
        /// </summary>
        private GraphicsResource ldrBuffer;

        /// <summary>
        /// The LensFlare instance from the library.
        /// </summary>
        private LensFlare lensFlare;

        private GraphicsResource intermediate;

        /// <summary>
        /// Timer to measure elapsed time.
        /// </summary>
        private Stopwatch timer = new Stopwatch();

        /// <summary>
        /// Frame time (in seconds) of the last frame.
        /// </summary>
        private double lastFrameTime;

        private TweakBar bar;

        private Scene scene;

        public Renderer(RenderForm window)
        {
            #if DEBUG
            var flags = DeviceCreationFlags.Debug;
            #else
            var flags = DeviceCreationFlags.None;
            #endif

            Device.CreateWithSwapChain(DriverType.Hardware, flags, new SwapChainDescription()
            {
                BufferCount = 3,
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

            ldrBuffer = new GraphicsResource(swapChain.GetBackBuffer<Texture2D>(0));

            device.ImmediateContext.ClearRenderTargetView(ldrBuffer.RTV, Color4.Black);
            swapChain.Present(0, PresentFlags.None);

            temporary = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true, true);
            hdrBuffer = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true);
            intermediate = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true);

            lensFlare = new LensFlare(device, RenderQuality.Medium, new OpticalProfile());

            scene = new Scene(device, window, window.ClientSize);

            timer.Start();

            TweakBar.InitializeLibrary(device);
            TweakBar.UpdateWindow(window);

            bar = new TweakBar(window, "Configuration Options");

            //bar.AddIntegerScalar("Testing", "min=5 max=50 group=Sponge keyincr=l keydecr=L", 42);
            bar.AddInteger("test", "Testing", "Group", 5, 15, 7, 1, "This is a help string");

            bar.AddFloat("exposure", "Exposure", "Group", 0, 3, 0.14, 0.01, 1, "");

            bar.AddBoolean("bool", "Boolean Test", "Group/Test", "ON", "OFF", true);

            bar.AddDirection("dir", "Direction Test", "Group 2", new Vector3(0.5f, 0, 0), "help!");

            bar.AddColor("col", "Color Test", "Group 2", new Color3(0.5f, 0, 0), "help!");
        }

        bool addFlares = true;

        /// <summary>
        /// Renders the scene to the backbuffer.
        /// </summary>
        public void Render()
        {
            if ((scene.isKeyPressed(SharpDX.DirectInput.Key.F))) addFlares = true;
            if ((scene.isKeyPressed(SharpDX.DirectInput.Key.R))) addFlares = false;

            RenderScene();

            if (addFlares) RenderLensFlares();

            Tonemap((Double)bar["exposure"]);

            TweakBar.Render();

            Present();
        }

        /// <summary>
        /// Renders the demonstration scene.
        /// </summary>
        private void RenderScene()
        {
            scene.Render(hdrBuffer.RTV);
        }

        /// <summary>
        /// Adds lens flare effects to the hdrBuffer.
        /// </summary>
        private void RenderLensFlares()
        {
            device.ImmediateContext.CopyResource(hdrBuffer.Resource, intermediate.Resource);

            double frameTime = (double)timer.ElapsedTicks / (double)Stopwatch.Frequency;
            lensFlare.Render(hdrBuffer.RTV, intermediate.SRV, frameTime - lastFrameTime);
            lastFrameTime = frameTime;
        }

        /// <summary>
        /// Tonemaps the hdrBuffer into the ldrBuffer (swapchain backbuffer) via
        /// a temporary texture for staging, using the Reinhard operator.
        /// </summary>
        private void Tonemap(double exposure)
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
            ", temporary.RTV, new[] { hdrBuffer.SRV }, null);

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
            ", ldrBuffer.RTV, new[] { temporary.SRV }, cbuffer);

            cbuffer.Dispose();
        }

        /// <summary>
        /// Presents the ldrBuffer to the display.
        /// </summary>
        public void Present()
        {
            swapChain.Present(1, PresentFlags.None);
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
                TweakBar.FinalizeLibrary();
                ldrBuffer.Dispose();
                hdrBuffer.Dispose();
                temporary.Dispose();
                lensFlare.Dispose();
                swapChain.Dispose();
                device.Dispose();
                timer.Stop();
            }
        }

        #endregion
    }
}
