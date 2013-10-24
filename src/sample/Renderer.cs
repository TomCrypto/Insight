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

        private TweakBar tweakBar;

        private Scene scene;

        private RenderForm window;

        private DeviceContext context;

        /// <summary>
        /// Called when the window is resized.
        /// </summary>
        private void ResizeWindow(object sender, EventArgs e)
        {
            context.ClearState();
            context.Flush();

            resolved.Dispose();
            temporary.Dispose();
            intermediate.Dispose();
            hdrBuffer.Dispose();
            ldrBuffer.Dispose();

            swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);

            InitializeResources();

            scene.Resize(window.ClientSize);
        }

        /// <summary>
        /// Initializes the graphics device and swapchain using
        /// the window provided in the Renderer constructor.
        /// </summary>
        private void InitializeGraphicsDevice()
        {
#if DEBUG
            var flags = DeviceCreationFlags.Debug;
#else
            var flags = DeviceCreationFlags.None;
#endif

            Device.CreateWithSwapChain(DriverType.Hardware, flags, new SwapChainDescription()
            {
                BufferCount = 2,
                IsWindowed = true,
                Flags = SwapChainFlags.None,
                OutputHandle = window.Handle,
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                SampleDescription = new SampleDescription(1, 0),
                ModeDescription = new ModeDescription()
                {
                    Format = Format.R8G8B8A8_UNorm,
                    Width = 0,//window.ClientSize.Width,
                    Height = 0,//window.ClientSize.Height,
                    RefreshRate = new Rational(60, 1),
                    Scaling = DisplayModeScaling.Centered,
                    ScanlineOrdering = DisplayModeScanlineOrder.Unspecified
                }
            }, out device, out swapChain);

            context = device.ImmediateContext;

            factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(window.Handle, WindowAssociationFlags.IgnoreAll);
        }

        private Factory factory;

        private GraphicsResource resolved;

        /// <summary>
        /// Initializes the graphics resources. Also
        /// presents once to avoid render ghosting.
        /// </summary>
        private void InitializeResources()
        {
            resolved     = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true);
            temporary    = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true, true);
            intermediate = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true);
            //hdrBuffer    = new GraphicsResource(device, window.ClientSize, Format.R32G32B32A32_Float, true, true);

            ldrBuffer    = new GraphicsResource(device, swapChain.GetBackBuffer<Texture2D>(0));

            context.ClearRenderTargetView(ldrBuffer.RTV, Color4.Black);
            swapChain.Present(0, PresentFlags.None);

            hdrBuffer = new GraphicsResource(device, new Texture2D(device, new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32G32B32A32_Float,
                Height = window.ClientSize.Height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                Width = window.ClientSize.Width
            }));
        }

        /// <summary>
        /// Called when the user changes the diffraction quality.
        /// </summary>
        private void QualityChange(object sender, Variable variable)
        {
            RenderQuality newQuality = (RenderQuality)((int)variable.Value - 1);

            if (Settings.quality != newQuality)
            {
                Settings.quality = newQuality;

                if (lensFlare != null) lensFlare.Dispose();

                lensFlare = new LensFlare(device, context, Settings.quality, new OpticalProfile());
            }
        }

        private void RotationChange(object sender, Variable variable)
        {
            Settings.rotationSensitivity = (float)((Double)variable.Value);
        }

        private void MovementChange(object sender, Variable variable)
        {
            Settings.movementSensitivity = (float)((Double)variable.Value);
        }

        /// <summary>
        /// Initializes our TweakBar.
        /// </summary>
        private void InitializeTweakBar()
        {
            if (!TweakBar.InitializeLibrary(device)) throw new System.Runtime.InteropServices.ExternalException("Failed to initialize AntTweakBar!");
            else
            {
                tweakBar = new TweakBar(window, "Configuration Options");

                tweakBar.AddFloat("gamma", "Gamma", "General", 1, 3, 2.2, 0.05, 3, "Gamma response to calibrate to the monitor.");
                tweakBar.AddFloat("exposure", "Exposure", "General", 0.01, 1.5, 0.015, 0.005, 3, "Exposure level at which to render the scene.");
                tweakBar.AddBoolean("diffraction", "Enable", "Diffraction", "Yes", "No", true, "Whether to display diffraction effects or not.");
                tweakBar.AddInteger("quality", "Quality", "Diffraction", 1, 4, (int)Settings.quality + 1, 1, "The quality of the diffraction effects (from 1 to 4).");
                tweakBar.AddFloat("fnumber", "f-number", "Diffraction", 1, 16, 1.5, 0.05, 2, "The f-number at which to model the aperture.");

                tweakBar.AddFloat("rotation_sensitivity", "Rotation", "Navigation", 0, 5, Settings.rotationSensitivity, 0.05, 2, "The sensitivity of mouse rotation.");
                tweakBar.AddFloat("movement_sensitivity", "Movement", "Navigation", 0, 1, Settings.movementSensitivity, 0.05, 2, "The sensitivity of keyboard movement.");

                tweakBar["quality"].VariableChange += QualityChange;

                tweakBar["rotation_sensitivity"].VariableChange += RotationChange;
                tweakBar["movement_sensitivity"].VariableChange += MovementChange;

                // put options here
            }
        }

        /// <summary>
        /// Initializes the Insight library.
        /// </summary>
        private void InitializeInsight()
        {
            // put config here

            lensFlare = new LensFlare(device, context, Settings.quality, new OpticalProfile());
        }

        /// <summary>
        /// Initializes the sample scene.
        /// </summary>
        private void InitializeScene()
        {
            scene = new Scene(device, context, window, window.ClientSize);
        }

        public Renderer(RenderForm window)
        {
            this.window = window;

            window.ResizeEnd += ResizeWindow;
            InitializeGraphicsDevice();
            InitializeResources();
            InitializeTweakBar();
            InitializeInsight();
            InitializeScene();
            timer.Start();
        }

        /// <summary>
        /// Renders the scene to the backbuffer.
        /// </summary>
        public void Render()
        {
            RenderScene();

            context.ResolveSubresource(hdrBuffer.Resource, 0, resolved.Resource, 0, Format.R32G32B32A32_Float);

            if ((Boolean)tweakBar["diffraction"].Value) RenderLensFlares();
            else context.CopyResource(resolved.Resource, intermediate.Resource);

            Tonemap((Double)tweakBar["exposure"].Value, (Double)tweakBar["gamma"].Value);

            TweakBar.Render();

            Present();
        }

        /// <summary>
        /// Renders the demonstration scene.
        /// </summary>
        private void RenderScene()
        {
            scene.Render(hdrBuffer.RTV, context);
        }

        /// <summary>
        /// Adds lens flare effects to the hdrBuffer.
        /// </summary>
        private void RenderLensFlares()
        {
            double frameTime = (double)timer.ElapsedTicks / (double)Stopwatch.Frequency;
            lensFlare.Render(intermediate.Dimensions, intermediate.RTV, resolved.SRV, ((Double)tweakBar["fnumber"].Value), frameTime - lastFrameTime);
            lastFrameTime = frameTime;
        }

        /// <summary>
        /// Tonemaps the hdrBuffer into the ldrBuffer (swapchain backbuffer) via
        /// a temporary texture for staging, using the Reinhard operator.
        /// </summary>
        private void Tonemap(double exposure, double gamma)
        {
            lensFlare.Pass.Pass(device, context, @"
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

                return float4(rgb, log(luminance(rgb) + 1e-6f));
            }
            ", temporary.Dimensions, temporary.RTV, new[] { intermediate.SRV }, null);

            context.GenerateMips(temporary.SRV);

            DataStream cbuffer = new DataStream(8, true, true);
            cbuffer.Write<float>((float)exposure);
            cbuffer.Write<float>(1.0f / (float)gamma);
            cbuffer.Position = 0;

            lensFlare.Pass.Pass(device, context, @"
            texture2D source             : register(t0);

            cbuffer constants : register(b0)
            {
                float exposure, invGamma;
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

                return float4(pow(rgb, invGamma), 1);
            }
            ", ldrBuffer.Dimensions, ldrBuffer.RTV, new[] { temporary.SRV }, cbuffer);

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
                lensFlare.Dispose();
                ldrBuffer.Dispose();
                hdrBuffer.Dispose();
                temporary.Dispose();
                intermediate.Dispose();
                resolved.Dispose();
                tweakBar.Dispose();
                scene.Dispose();

                TweakBar.FinalizeLibrary();

                context.ClearState();
                context.Flush();

                device.Dispose();
                context.Dispose();
                swapChain.Dispose();
                factory.Dispose();
                timer.Stop();
            }
        }

        #endregion
    }
}
